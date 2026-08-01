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
        [HarmonyPatch("Update")]
        public static void PlayerCorpse_Update_Postfix(PlayerCorpse __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);
            var counter = dyn.Get<float>("ghostSpawnCounter");

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
