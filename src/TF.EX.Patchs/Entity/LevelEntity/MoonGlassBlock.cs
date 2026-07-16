using HarmonyLib;
using TF.EX.Domain;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.MoonGlassBlock))]
    internal class MoonGlassBlockPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("InsideExplode")]
        public static void MoonGlassBlock_InsideExplode(TowerFall.MoonGlassBlock __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsInit())
            {
                return;
            }

            MoonGlassBlockExplodeController.StartExplode(__instance);
        }
    }
}
