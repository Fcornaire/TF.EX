using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(PlayerGhost))]
    public class PlayerGhostPatch
    {
        private const int ST_SPAWN = 0;
        private const int ST_NORMAL = 1;
        private const int ST_DODGE = 2;
        private const int ST_DEAD = 3;
        private const int ST_DESPAWN = 4;

        private const float SPAWN_FRAMES = 30f;
        private const float DODGE_FRAMES = 10f;
        private const float DEAD_FRAMES = 30f;
        private const float DESPAWN_FRAMES = 30f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, typeof(PlayerCorpse))]
        public static void PlayerGhost_ctor(PlayerGhost __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Add("ghostStateCounter", 0f);
            dyn.Add("ghostDespawnStart", Vector2.Zero);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Enemy), "SetState")]
        public static void Enemy_SetState_Postfix(Enemy __instance)
        {
            if (__instance is PlayerGhost)
            {
                DynamicData.For(__instance).Set("ghostStateCounter", 0f);
            }
            else if (__instance is Slime || __instance is Bat)
            {
                var dyn = DynamicData.For(__instance);
                dyn.Set("enemyStateCounter", 0f);
                dyn.Set("enemyStatePhase", 0);

                if (__instance is Bat)
                {
                    dyn.Set("enemySwoopIndex", 0);
                }

                if (!ServiceCollections.ResolveNetplayManager().IsInit())
                {
                    return;
                }

                dyn.Get<Coroutine>("coroutine")?.Cancel();

                if (__instance is Slime slime && slime.State == 2)
                {
                    slime.Speed.X = 0f;

                    var slimeColor = dyn.Get<Slime.SlimeColors>("slimeColor");
                    if (slimeColor == Slime.SlimeColors.Red || slimeColor == Slime.SlimeColors.Blue)
                    {
                        Sounds.en_redSlimePreJump.Play(slime.X);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DespawnEnter")]
        public static void PlayerGhost_DespawnEnter_Postfix(PlayerGhost __instance)
        {
            if (!ServiceCollections.ResolveNetplayManager().IsInit())
            {
                return;
            }

            DynamicData.For(__instance).Set("ghostDespawnStart", __instance.Position);

            __instance.DeleteAllComponents<Tween>();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void PlayerGhost_Update_Postfix(PlayerGhost __instance)
        {
            if (!ServiceCollections.ResolveNetplayManager().IsInit())
            {
                return;
            }

            var dyn = DynamicData.For(__instance);

            dyn.Get<Coroutine>("coroutine")?.Cancel();

            var counter = dyn.Get<float>("ghostStateCounter") + Monocle.Engine.TimeMult;
            dyn.Set("ghostStateCounter", counter);

            switch (dyn.Get<int>("currentState"))
            {
                case ST_SPAWN:
                    if (counter >= SPAWN_FRAMES)
                    {
                        __instance.State = ST_NORMAL;
                    }
                    break;

                case ST_DODGE:
                    if (counter >= DODGE_FRAMES)
                    {
                        __instance.State = ST_NORMAL;
                    }
                    break;

                case ST_DEAD:
                    if (counter >= DEAD_FRAMES)
                    {
                        __instance.RemoveSelf();
                    }
                    break;

                case ST_DESPAWN:
                    UpdateDespawn(__instance, dyn, counter);
                    break;
            }
        }

        private static void UpdateDespawn(PlayerGhost self, DynamicData dyn, float counter)
        {
            var despawnCorpse = dyn.Get<PlayerCorpse>("despawnCorpse");

            if (despawnCorpse == null)
            {
                self.RemoveSelf();
                return;
            }

            var start = dyn.Get<Vector2>("ghostDespawnStart");
            var percent = Math.Min(counter / DESPAWN_FRAMES, 1f);

            self.Position = Vector2.Lerp(start, despawnCorpse.Position, Ease.CubeIn(percent));

            if (counter >= DESPAWN_FRAMES)
            {
                self.RemoveSelf();
            }
        }
    }
}
