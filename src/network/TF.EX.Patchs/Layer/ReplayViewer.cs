using HarmonyLib;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    [HarmonyPatch(typeof(ReplayViewer))]
    internal class ReplayViewerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Watch")]
        public static bool ReplayViewer_Watch(Action onComplete)
        {
            onComplete();
            return false;
        }
    }
}
