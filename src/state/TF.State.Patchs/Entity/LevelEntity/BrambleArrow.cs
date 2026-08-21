using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Domain.Models;
using TF.State.TowerFallExtensions.Entity.LevelEntity;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(BrambleArrow))]
    public class BrambleArrowPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("HitWall")]
        public static void HitWall_Prefix(BrambleArrow __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dyn = DynamicData.For(__instance);

            if (dyn.Get<bool>("used"))
            {
                return;
            }

            int id = dyn.Get<double>("actualDepth").GetHashCode();

            var spread = BrambleSpreadController.Start(id, __instance.Position, __instance.PlayerIndex);
            dyn.Set("BrambleSpread", spread);

            dyn.Set("used", true);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Update_Postfix(BrambleArrow __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            if (StateFlags.IsRestoring)
            {
                return;
            }

            var spread = DynamicData.For(__instance).Get<BrambleSpreadState>("BrambleSpread");
            if (spread != null && !spread.IsComplete)
            {
                BrambleSpreadController.Step(__instance, spread);
            }
        }
    }
}
