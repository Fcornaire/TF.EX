using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Session))]
    internal static class SessionRoundEndPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("GotoNextRound")]
        public static bool Session_GotoNextRound()
        {
            if (!StandalonePlayback.IsActive)
            {
                return true;
            }

            var service = ServiceCollections.ResolveReplayService();

            return service == null || service.PlaybackFrame <= service.LastFrame;
        }
    }
}
