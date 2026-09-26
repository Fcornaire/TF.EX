using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.Replay.Domain;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    [HarmonyPatch(typeof(Level))]
    internal static class LevelPlaybackOverlay
    {
        [HarmonyPostfix]
        [HarmonyPatch("PostScreen")]
        public static void Level_PostScreen()
        {
            var service = ServiceCollections.ResolveReplayService();

            if (service == null || !service.IsPlayback || !TFGame.GameLoaded)
            {
                return;
            }

            if (GifExport.IsCapturing || PlaybackControls.HideOverlay)
            {
                return;
            }

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Monocle.Engine.Instance.Screen.Matrix);

            try
            {
                Render(service);
            }
            finally
            {
                Draw.SpriteBatch.End();
            }
        }

        private static void Render(Domain.Ports.IReplayService service)
        {
            InputDisplayerOverlay.Render(service);

            SeekBar.Render(service.PlaybackFrame, service.LastFrame, PlaybackControls.IsPaused, PlaybackControls.HoverFrame, service.SeekBlockedBy);

            if (Takeover.State != Takeover.Phase.Off && service.SeekBlockedBy == null)
            {
                SeekBar.RenderTakeoverMark(
                    Takeover.State == Takeover.Phase.Countdown ? service.PlaybackFrame : Takeover.StartFrame,
                    service.LastFrame);
            }

            ControlsHelp.Render(PlaybackControls.ShouldShowHelp);

            TakeoverOverlay.Render();

            SeatPicker.Render(service);

            if (PlaybackControls.MousePosition.HasValue)
            {
                SeekBar.RenderCursor(PlaybackControls.MousePosition.Value);
            }
        }
    }
}
