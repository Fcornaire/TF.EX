using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Ice))]
    internal class IcePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Ice_Update(TowerFall.Ice __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var dyn = DynamicData.For(__instance);
            var oldTilesAlpha = dyn.Get<float>("oldTilesAlpha") - 0.1f * Monocle.Engine.TimeMult;
            dyn.Set("oldTilesAlpha", oldTilesAlpha);

            if (oldTilesAlpha <= 0f)
            {
                dyn.Set("oldTiles", null);
                __instance.Active = false;
            }

            return false;
        }
    }
}
