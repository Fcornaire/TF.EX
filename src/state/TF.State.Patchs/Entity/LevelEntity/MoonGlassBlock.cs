using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity.LevelEntity;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.MoonGlassBlock))]
    internal class MoonGlassBlockPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("InsideExplode")]
        public static void MoonGlassBlock_InsideExplode(TowerFall.MoonGlassBlock __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            MoonGlassBlockExplodeController.StartExplode(__instance);
        }
    }
}
