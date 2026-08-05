using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Layer
{
    [HarmonyPatch(typeof(TowerFall.LightingLayer))]
    internal class LightingLayerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool LightingLayer_Update()
        {
            if (!StateFlags.IsRollbackFrame && !StateFlags.HasFramesToReSimulate)
            {
                return true;
            }

            if (TowerFall.TFGame.Instance?.Scene is TowerFall.Level level && !level.Frozen)
            {
                return false;
            }

            return true;
        }
    }
}
