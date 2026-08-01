using HarmonyLib;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(ReplayRecorder))]
    internal class ReplayRecorderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ClearFrames")]
        public static bool ReplayRecorder_ClearFrames(ReplayRecorder __instance)
        {
            var netplayManager = TF.EX.Domain.ServiceCollections.ResolveNetplayManager();

            var mode = MainMenu.VersusMatchSettings.Mode;

            if (mode.IsNetplay() || netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Test || netplayManager.IsReplayMode())
            {
                return false;
            }

            return true;
        }
    }
}
