using HarmonyLib;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    [HarmonyPatch(typeof(ReplayViewer))]
    internal class ReplayViewerPatch
    {
        /// <summary>
        /// Skip the replay viewer
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("Watch")]
        public static bool ReplayViewer_Watch(Action onComplete)
        {
            onComplete();
            return false;
        }
    }
}
