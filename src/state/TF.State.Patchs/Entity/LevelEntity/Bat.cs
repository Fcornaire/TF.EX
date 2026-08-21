using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Bat))]
    public class BatPatch
    {
        private const int ST_IDLE = 0;
        private const int ST_SWOOP = 1;
        private const int ST_DEAD = 2;

        private const int SWOOP_PHASE_AIM = 0;
        private const int SWOOP_PHASE_DIVE = 1;
        private const int SWOOP_PHASE_RECOVER = 2;

        private const int DEAD_PHASE_SETTLE = 0;
        private const int DEAD_PHASE_FADE = 1;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Facing), typeof(Bat.BatType)])]
        public static void Bat_ctor(Bat __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("enemyStateCounter", 0f);
            dyn.Add("enemyStatePhase", 0);
            dyn.Add("enemySwoopIndex", 0);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Bat_Update_Postfix(Bat __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            dyn.Get<Coroutine>("coroutine")?.Cancel();

            var counter = dyn.Get<float>("enemyStateCounter") + Monocle.Engine.TimeMult;
            dyn.Set("enemyStateCounter", counter);

            switch (dyn.Get<int>("currentState"))
            {
                case ST_IDLE:
                    UpdateIdle(__instance, dyn, counter);
                    break;
                case ST_SWOOP:
                    UpdateSwoop(__instance, dyn, counter);
                    break;
                case ST_DEAD:
                    UpdateDead(__instance, dyn, counter);
                    break;
            }
        }

        private static void UpdateIdle(Bat self, DynamicData dyn, float counter)
        {
            if (counter < 10f)
            {
                return;
            }

            dyn.Set("enemyStateCounter", 5f);

            if (dyn.Get<Counter>("swoopCooldown"))
            {
                return;
            }

            var target = self.GetClosestPlayerWithSight(14400f);
            if (target != null
                && WrapMath.AbsDiffX(self.X, target.X) <= (float)dyn.Get<int>("detectXRange")
                && WrapMath.DiffY(self.Y, target.Y) >= -10f)
            {
                dyn.Set("target", target);
                self.State = ST_SWOOP;
            }
        }

        private static void UpdateSwoop(Bat self, DynamicData dyn, float counter)
        {
            var batType = dyn.Get<Bat.BatType>("batType");
            var target = GetLiveTarget(dyn);

            switch (dyn.Get<int>("enemyStatePhase"))
            {
                case SWOOP_PHASE_AIM:
                    if (target != null)
                    {
                        var diffX = WrapMath.DiffX(self.X, target.X);
                        if (diffX != 0f)
                        {
                            self.Facing = (Facing)System.Math.Sign(diffX);
                        }

                        WrapMath.ShortestOpen(self.Level, self.Position, target.Position, out var swoopTargetSpeed);
                        dyn.Set("swoopTargetSpeed", swoopTargetSpeed.SafeNormalize(-0.3f));
                    }

                    if (batType == Bat.BatType.Bird && dyn.Get<int>("enemySwoopIndex") == 0)
                    {
                        Sounds.en_crowPreDive.Play(self.X);
                    }

                    dyn.Set("enemyStatePhase", SWOOP_PHASE_DIVE);
                    dyn.Set("enemyStateCounter", 0f);
                    break;

                case SWOOP_PHASE_DIVE:
                    if (counter < 10f)
                    {
                        return;
                    }

                    SpawnAttack(self, dyn, batType);

                    var current = dyn.Get<Vector2>("swoopTargetSpeed");
                    var swoopSpeed = dyn.Get<float>("swoopSpeed");
                    if (target == null
                        || !WrapMath.ShortestOpen(self.Level, self.Position, target.Position, out var shortest)
                        || shortest.LengthSquared() >= 6400f)
                    {
                        dyn.Set("swoopTargetSpeed", current.SafeNormalize(0f - swoopSpeed));
                    }
                    else
                    {
                        dyn.Set("swoopTargetSpeed", shortest.SafeNormalize(swoopSpeed));
                    }

                    dyn.Set("enemyStatePhase", SWOOP_PHASE_RECOVER);
                    dyn.Set("enemyStateCounter", 0f);
                    break;

                case SWOOP_PHASE_RECOVER:
                    if (counter < 20f)
                    {
                        return;
                    }

                    var swoops = batType == Bat.BatType.Bird ? 2 : 1;
                    var swoopIndex = dyn.Get<int>("enemySwoopIndex") + 1;
                    if (swoopIndex < swoops
                        && target != null
                        && !(WrapMath.DiffY(self.Y, target.Y) < -20f || !self.CanSeePlayer(target)))
                    {
                        dyn.Set("enemySwoopIndex", swoopIndex);
                        dyn.Set("enemyStatePhase", SWOOP_PHASE_AIM);
                        dyn.Set("enemyStateCounter", 0f);
                    }
                    else
                    {
                        self.State = ST_IDLE;
                    }
                    break;
            }
        }

        private static void SpawnAttack(Bat self, DynamicData dyn, Bat.BatType batType)
        {
            switch (batType)
            {
                default:
                    Sounds.en_eyeBatSwoop.Play(self.X);
                    EnemyAttack.Spawn(self.Level, self, new Vector2(5f, 5f), 10, 10, 18);
                    break;
                case Bat.BatType.Bomb:
                    Sounds.en_bombBatSwoop.Play(self.X);
                    EnemyAttack.Spawn(self.Level, self, new Vector2(5f, 5f), 10, 10, 18, () => dyn.Invoke("Explode"));
                    break;
                case Bat.BatType.SuperBomb:
                    Sounds.en_superBombBatSwoop.Play(self.X);
                    EnemyAttack.Spawn(self.Level, self, new Vector2(5f, 5f), 10, 10, 18, () => dyn.Invoke("Explode"));
                    break;
                case Bat.BatType.Bird:
                    if (dyn.Get<int>("enemySwoopIndex") == 0)
                    {
                        Sounds.en_crowDive1.Play(self.X);
                    }
                    else
                    {
                        Sounds.en_crowDive2.Play(self.X);
                    }
                    EnemyAttack.Spawn(self.Level, self, new Vector2(6f, 5f), 12, 10, 18);
                    break;
            }
        }

        private static void UpdateDead(Bat self, DynamicData dyn, float counter)
        {
            var batType = dyn.Get<Bat.BatType>("batType");
            if (batType == Bat.BatType.Bomb || batType == Bat.BatType.SuperBomb)
            {
                if (counter >= 10f)
                {
                    dyn.Invoke("Explode");
                }
                return;
            }

            switch (dyn.Get<int>("enemyStatePhase"))
            {
                case DEAD_PHASE_SETTLE:
                    if (counter >= 60f && dyn.Get<ArrowCushion>("arrowCushion").Count == 0)
                    {
                        dyn.Set("enemyStatePhase", DEAD_PHASE_FADE);
                        dyn.Set("enemyStateCounter", 0f);
                    }
                    break;

                case DEAD_PHASE_FADE:
                    var sprite = dyn.Get<Monocle.Sprite<string>>("sprite");
                    sprite.Color = Color.White * (1f - System.Math.Min(counter / 120f, 1f));
                    if (counter >= 120f)
                    {
                        self.RemoveSelf();
                    }
                    break;
            }
        }

        private static Player GetLiveTarget(DynamicData dyn)
        {
            var target = dyn.Get<Player>("target");
            return target != null && target.Scene != null ? target : null;
        }
    }
}
