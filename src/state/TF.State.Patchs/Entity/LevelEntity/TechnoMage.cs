using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TechnoMage))]
    public class TechnoMagePatch
    {
        private const int STATE_PATROL = 0;
        private const int STATE_ALERT = 1;
        private const int STATE_DEAD = 2;

        private const int ALERT_PHASE_READY = 0;
        private const int ALERT_PHASE_FIRE = 1;
        private const int ALERT_PHASE_COOLDOWN = 2;

        private const int DEAD_PHASE_WAIT = 0;
        private const int DEAD_PHASE_DONE = 1;

        private const float ALERT_DELAY = 20f;
        private const float SHOT_INTERVAL = 60f;
        private const float VOLLEY_COOLDOWN = 90f;
        private const int SHOTS_PER_VOLLEY = 2;
        private const float DEAD_DELAY = 60f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Facing)])]
        public static void TechnoMage_ctor(TechnoMage __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("enemyStateCounter", 0f);
            dyn.Add("enemyStatePhase", 0);
            dyn.Add("enemyShotIndex", 0);
            dyn.Add("enemyTrackedState", STATE_PATROL);
        }

        [HarmonyPrefix]
        [HarmonyPatch("AlertUpdate")]
        public static bool TechnoMage_AlertUpdate(TechnoMage __instance, ref int __result)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            var target = GetLiveTarget(dyn);
            var targetSpeed = dyn.Get<Vector2>("targetSpeed");

            targetSpeed.X = target != null && WrapMath.AbsDiffX(__instance.X, target.X) < 40f
                ? WrapMath.SignX(target.X, __instance.X) * 0.3f
                : 0f;
            targetSpeed.Y = dyn.Get<SineWave>("moveSine").Value * 0.1f;
            dyn.Set("targetSpeed", targetSpeed);

            __result = STATE_ALERT;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void TechnoMage_Update_Postfix(TechnoMage __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            __instance.DeleteAllComponents<Coroutine>();

            var dyn = DynamicData.For(__instance);
            var state = dyn.Get<int>("currentState");

            if (state != dyn.Get<int>("enemyTrackedState"))
            {
                dyn.Set("enemyTrackedState", state);
                dyn.Set("enemyStatePhase", 0);
                dyn.Set("enemyStateCounter", 0f);
                dyn.Set("enemyShotIndex", 0);

                if (state == STATE_ALERT)
                {
                    var entered = GetLiveTarget(dyn);
                    if (entered != null)
                    {
                        __instance.SetFacing(entered.Position);
                    }
                }
            }

            var counter = dyn.Get<float>("enemyStateCounter") + Engine.TimeMult;
            dyn.Set("enemyStateCounter", counter);

            switch (state)
            {
                case STATE_ALERT:
                    UpdateAlert(__instance, dyn, counter);
                    break;
                case STATE_DEAD:
                    UpdateDead(__instance, dyn, counter);
                    break;
            }
        }

        private static void UpdateAlert(TechnoMage self, DynamicData dyn, float counter)
        {
            switch (dyn.Get<int>("enemyStatePhase"))
            {
                case ALERT_PHASE_READY:
                    if (counter >= ALERT_DELAY)
                    {
                        dyn.Set("enemyStatePhase", ALERT_PHASE_FIRE);
                        dyn.Set("enemyStateCounter", SHOT_INTERVAL);
                    }
                    break;

                case ALERT_PHASE_FIRE:
                    if (counter < SHOT_INTERVAL)
                    {
                        return;
                    }

                    if (!TryFire(self, dyn))
                    {
                        self.State = STATE_PATROL;
                        return;
                    }

                    var shotIndex = dyn.Get<int>("enemyShotIndex") + 1;
                    dyn.Set("enemyShotIndex", shotIndex);
                    dyn.Set("enemyStateCounter", 0f);

                    if (shotIndex >= SHOTS_PER_VOLLEY)
                    {
                        dyn.Set("enemyStatePhase", ALERT_PHASE_COOLDOWN);
                    }
                    break;

                case ALERT_PHASE_COOLDOWN:
                    if (counter >= SHOT_INTERVAL + VOLLEY_COOLDOWN)
                    {
                        dyn.Set("enemyShotIndex", 0);
                        dyn.Set("enemyStatePhase", ALERT_PHASE_FIRE);
                        dyn.Set("enemyStateCounter", SHOT_INTERVAL);
                    }
                    break;
            }
        }

        private static bool TryFire(TechnoMage self, DynamicData dyn)
        {
            var target = GetLiveTarget(dyn);
            if (target == null || target.Dead || !self.CanSeePlayer(target))
            {
                return false;
            }

            var direction = WrapMath.Shortest(self.Position, target.Position).SafeNormalize();
            if (self.CollideCheck(GameTags.Solid, self.Position + direction * 20f))
            {
                return false;
            }

            Sounds.en_technoMageFire.Play(self.X);
            self.Level.Add(new TechnoMage.TechnoMissile(self.Position, direction, target));
            dyn.Get<Sprite<string>>("sprite").Play("attack");
            self.SetFacing(target.Position);
            return true;
        }

        private static void UpdateDead(TechnoMage self, DynamicData dyn, float counter)
        {
            if (dyn.Get<int>("enemyStatePhase") != DEAD_PHASE_WAIT)
            {
                return;
            }

            if (counter >= DEAD_DELAY)
            {
                dyn.Set("enemyStatePhase", DEAD_PHASE_DONE);
                dyn.Invoke("Explode");
            }
        }

        private static Player GetLiveTarget(DynamicData dyn)
        {
            var target = dyn.Get<Player>("target");
            return target != null && target.Scene != null ? target : null;
        }
    }
}
