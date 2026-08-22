using HarmonyLib;
using TF.EX.Domain;
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
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var isExDriven = NetplayOptions.IsApplied
                || netplayManager.IsInit()
                || netplayManager.IsReplayMode()
                || netplayManager.IsTestMode();

            if (!isExDriven)
            {
                return true;
            }

            onComplete();
            return false;
        }
    }
}
