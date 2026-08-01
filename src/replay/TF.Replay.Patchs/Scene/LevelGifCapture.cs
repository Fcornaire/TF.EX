using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    internal static class LevelGifCapture
    {
        [HarmonyPostfix]
        [HarmonyPatch("PostScreen")]
        public static void Level_PostScreen()
        {
            if (!GifExport.IsCapturing)
            {
                return;
            }

            var service = ServiceCollections.ResolveReplayService();

            if (service == null || !service.IsPlayback)
            {
                return;
            }

            GifExport.CaptureFrame(service.PlaybackFrame);
        }
    }
}
