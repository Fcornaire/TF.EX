using HarmonyLib;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Layer
{
    [HarmonyPatch(typeof(GameplayLayer))]
    internal static class GameplayLayerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("BatchedRender")]
        public static void GameplayLayer_BatchedRender()
        {
            var service = ServiceCollections.ResolveReplayService();

            if (service == null || !service.IsPlayback || !TFGame.GameLoaded)
            {
                return;
            }

            if (GifExport.IsCapturing)
            {
                return;
            }

            InputDisplayerOverlay.Render(service);

            SeekBar.Render(service.PlaybackFrame, service.LastFrame,PlaybackControls.IsPaused, PlaybackControls.HoverFrame, service.SeekBlockedBy);

            ControlsHelp.Render(PlaybackControls.ShowHelp);

            if (PlaybackControls.MousePosition.HasValue)
            {
                SeekBar.RenderCursor(PlaybackControls.MousePosition.Value);
            }
        }
    }
}
