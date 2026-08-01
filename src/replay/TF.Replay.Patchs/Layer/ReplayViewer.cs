using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Layer
{
    [HarmonyPatch(typeof(ReplayViewer))]
    internal static class ReplayViewerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Watch")]
        public static bool ReplayViewer_Watch(Action onComplete)
        {
            if (!StandalonePlayback.IsActive)
            {
                return true;
            }

            onComplete();
            return false;
        }
    }
}
