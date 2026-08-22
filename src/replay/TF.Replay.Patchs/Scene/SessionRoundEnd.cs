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
            var service = ServiceCollections.ResolveReplayService();

            if (service == null || !service.IsPlayback)
            {
                return true;
            }

            if (Takeover.SuppressesPlaybackChecks)
            {
                return false;
            }

            if (!StandalonePlayback.IsActive)
            {
                return true;
            }

            return service.PlaybackFrame <= service.LastFrame;
        }
    }
}
