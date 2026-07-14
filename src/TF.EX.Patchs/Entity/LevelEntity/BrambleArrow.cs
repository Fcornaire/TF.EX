using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(BrambleArrow))]
    public class BrambleArrowPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("HitWall")]
        public static void HitWall_Prefix(BrambleArrow __instance)
        {
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
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (netplayManager.IsUpdating())
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
