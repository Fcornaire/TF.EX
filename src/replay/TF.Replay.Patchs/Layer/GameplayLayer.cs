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

            if (Takeover.State != Takeover.Phase.Off && service.SeekBlockedBy == null)
            {
                SeekBar.RenderTakeoverMark(
                    Takeover.State == Takeover.Phase.Countdown ? service.PlaybackFrame : Takeover.StartFrame,
                    service.LastFrame);
            }

            ControlsHelp.Render(PlaybackControls.ShowHelp);

            TakeoverOverlay.Render();

            SeatPicker.Render(service);

            if (PlaybackControls.MousePosition.HasValue)
            {
                SeekBar.RenderCursor(PlaybackControls.MousePosition.Value);
            }
        }
    }
}
