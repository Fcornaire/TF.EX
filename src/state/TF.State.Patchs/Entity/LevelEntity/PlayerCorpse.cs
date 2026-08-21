using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(PlayerCorpse))]
    public class PlayerCorpsePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(string), typeof(Allegiance), typeof(Microsoft.Xna.Framework.Vector2), typeof(Facing), typeof(int), typeof(int)])]
        public static void PlayerCorpse_ctor_Postfix(PlayerCorpse __instance)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Set("prismTicks", -1f);
            dyn.Set("brambleTicks", -1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Added")]
        public static void PlayerCorpse_Added_Postfix(PlayerCorpse __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            dyn.Set("ghostCoroutine", null);

            var spawnsGhost = __instance.PlayerIndex != -1
                && __instance.Level.Session.MatchSettings.Variants.ReturnAsGhosts[__instance.PlayerIndex];

            dyn.Set("ghostSpawnCounter", spawnsGhost ? 0f : -1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("DieByArrow")]
        public static void PlayerCorpse_DieByArrow_Postfix(PlayerCorpse __instance, Arrow arrow)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);

            if (arrow is BrambleArrow)
            {
                var dropAlarm = dyn.Get<Monocle.Alarm>("dropArrowAlarm");
                foreach (var component in __instance.Components.OfType<Monocle.Alarm>().Where(a => a != dropAlarm).ToList())
                {
                    __instance.Remove(component);
                }

                dyn.Set("brambleTicks", 0f);
            }

            var prismCoroutine = dyn.Get<Monocle.Coroutine>("prismCoroutine");
            if (prismCoroutine != null)
            {
                __instance.Remove(prismCoroutine);
                dyn.Set("prismCoroutine", null);
                dyn.Set("prismTicks", 0f);
            }
        }

        private static void StepOwnedSequences(PlayerCorpse corpse, DynamicData dyn)
        {
            var prismTicks = dyn.Get("prismTicks") as float? ?? -1f;
            if (corpse.PrismHit && prismTicks >= 0f)
            {
                var next = prismTicks + Monocle.Engine.TimeMult;
                dyn.Set("prismTicks", next);

                if (prismTicks < 5f && next >= 5f)
                {
                    dyn.Set("prismFall", true);
                    corpse.Speed.X *= 0.3f;
                }

                if (prismTicks < 15f && next >= 15f)
                {
                    corpse.ArrowCushion.ReleaseArrows(corpse.Speed);
                    Sounds.sfx_corpseVanish.Play(corpse.X);
                }

                if (prismTicks < 30f && next >= 30f)
                {
                    corpse.RemoveSelf();
                    return;
                }
            }

            var brambleTicks = dyn.Get("brambleTicks") as float? ?? -1f;
            if (brambleTicks >= 0f)
            {
                if (dyn.Get<Monocle.FlashingImage[]>("brambles") == null)
                {
                    dyn.Set("brambleTicks", -1f);
                    return;
                }

                var next = brambleTicks + Monocle.Engine.TimeMult;
                dyn.Set("brambleTicks", next);

                if (brambleTicks < 10f && next >= 10f)
                {
                    dyn.Set("bramblesVisible", true);
                }

                if (brambleTicks < 30f && next >= 30f)
                {
                    dyn.Set("BrambleCollidable", true);
                }

                if (brambleTicks < 610f && next >= 610f)
                {
                    dyn.Set("BrambleCollidable", false);
                }

                if (brambleTicks < 630f && next >= 630f)
                {
                    dyn.Set("bramblesVisible", false);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void PlayerCorpse_Update_Postfix(PlayerCorpse __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);

            StepOwnedSequences(__instance, dyn);

            if (__instance.MarkedForRemoval)
            {
                return;
            }

            var counter = dyn.Get("ghostSpawnCounter") as float? ?? -1f;

            if (counter < 0f)
            {
                return;
            }

            counter += Monocle.Engine.TimeMult;
            dyn.Set("ghostSpawnCounter", counter);

            if (counter < (dyn.Get<bool>("CanExplode") ? 55f : 60f))
            {
                return;
            }

            if (__instance.PrismHit)
            {
                dyn.Set("ghostSpawnCounter", -1f);
                return;
            }

            if (__instance.Squished != Microsoft.Xna.Framework.Vector2.Zero || __instance.Revived)
            {
                return;
            }

            __instance.Level.Add(new PlayerGhost(__instance));
            dyn.Set("ghostSpawnCounter", -1f);
        }

        [HarmonyPrefix]
        [HarmonyPatch("StartDroppingArrows")]
        public static void PlayerCorpse_StartDroppingArrows_Prefix(PlayerCorpse __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            var existing = dyn.Get<Monocle.Alarm>("dropArrowAlarm");
            if (existing != null)
            {
                __instance.Remove(existing);
                dyn.Set("dropArrowAlarm", null);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("DropNextArrow")]
        public static void PlayerCorpse_DropNextArrow_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("DropNextArrow")]
        public static void PlayerCorpse_DropNextArrow_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }
    }
}
