using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class SeekBar
    {
        private const float TrackWidth = 180f;
        private const float TrackY = 15f;
        private const float TrackHeight = 3f;
        private const float LabelY = 7f;
        private const float StatusY = 26f;
        private const float EndedY = 35f;
        private const float HeadWidth = 2f;
        private const float HeadHeight = 6f;

        private const float HitTop = TrackY - 8f;
        private const float HitBottom = TrackY + TrackHeight + 8f;

        private static float CenterX => 160f + Overlay.CenterOffset;
        private static float TrackLeft => CenterX - TrackWidth / 2f;

        public static bool Contains(Vector2 point)
            => point.X >= TrackLeft - 2f && point.X <= TrackLeft + TrackWidth + 2f
               && point.Y >= HitTop && point.Y <= HitBottom;

        public static int FrameAt(float x, int lastFrame)
        {
            var ratio = (x - TrackLeft) / TrackWidth;

            return Math.Clamp((int)MathF.Round(ratio * lastFrame), 0, lastFrame);
        }

        public static void RenderCursor(Vector2 at)
        {
            if (at.X < -4f || at.X > 324f + Overlay.RightOffset || at.Y < -4f || at.Y > 244f)
            {
                return;
            }

            Draw.Rect(at.X - 4f, at.Y - 1f, 9f, 3f, Color.Black * 0.75f);
            Draw.Rect(at.X - 1f, at.Y - 4f, 3f, 9f, Color.Black * 0.75f);
            Draw.Rect(at.X - 3f, at.Y, 7f, 1f, Color.White);
            Draw.Rect(at.X, at.Y - 3f, 1f, 7f, Color.White);
        }

        public static void Render(int frame, int lastFrame, bool paused, int? hoverFrame, string seekBlockedBy = null)
        {
            if (lastFrame <= 0)
            {
                return;
            }

            if (seekBlockedBy != null)
            {
                Draw.OutlineTextCentered(TFGame.Font, $"{frame} / {lastFrame}{(paused ? "  PAUSED" : "")}",
                    new Vector2(CenterX, LabelY), Color.White, Color.Black);

                Draw.OutlineTextCentered(TFGame.Font, $"NO SEEKING - {seekBlockedBy.ToUpperInvariant()} SAVES NO STATE",
                    new Vector2(CenterX, TrackY + 2f), Color.Gold, Color.Black);

                RenderStatus();
                return;
            }

            var current = Math.Clamp(frame, 0, lastFrame);
            var progress = (float)current / lastFrame;

            Draw.Rect(TrackLeft - 2f, TrackY - 2f, TrackWidth + 4f, TrackHeight + 4f, Color.White * 0.55f);
            Draw.Rect(TrackLeft - 1f, TrackY - 1f, TrackWidth + 2f, TrackHeight + 2f, Color.Black);
            Draw.Rect(TrackLeft, TrackY, TrackWidth, TrackHeight, Color.White * 0.3f);

            RenderSelection(lastFrame);

            Draw.Rect(TrackLeft, TrackY, TrackWidth * progress, TrackHeight, Color.White);

            if (hoverFrame.HasValue)
            {
                var hoverX = TrackLeft + TrackWidth * ((float)hoverFrame.Value / lastFrame);

                Draw.Rect(hoverX - 1f, TrackY - 4f, 3f, TrackHeight + 8f, Color.Black * 0.75f);
                Draw.Rect(hoverX, TrackY - 3f, 1f, TrackHeight + 6f, Color.Yellow);
            }

            var headX = TrackLeft + TrackWidth * progress - HeadWidth / 2f;
            var headY = TrackY + TrackHeight / 2f - HeadHeight / 2f;

            Draw.Rect(headX - 1f, headY - 1f, HeadWidth + 2f, HeadHeight + 2f, Color.Black * 0.75f);
            Draw.Rect(headX, headY, HeadWidth, HeadHeight, Color.White);

            var label = hoverFrame.HasValue
                ? $"{current} / {lastFrame}  >> {hoverFrame.Value}"
                : paused
                    ? $"{current} / {lastFrame}  PAUSED"
                    : $"{current} / {lastFrame}";

            Draw.OutlineTextCentered(TFGame.Font, label, new Vector2(CenterX, LabelY), Color.White, Color.Black);

            if (frame > lastFrame)
            {
                Draw.OutlineTextCentered(TFGame.Font, "THE REPLAY IS OVER",
                    new Vector2(CenterX, EndedY), Color.Gold, Color.Black);
            }

            RenderStatus();
        }

        private static void RenderSelection(int lastFrame)
        {
            if (PlaybackControls.MarkIn == null)
            {
                return;
            }

            var inX = TrackLeft + TrackWidth * (Math.Clamp(PlaybackControls.MarkIn.Value, 0, lastFrame) / (float)lastFrame);
            var outX = PlaybackControls.MarkOut.HasValue
                ? TrackLeft + TrackWidth * (Math.Clamp(PlaybackControls.MarkOut.Value, 0, lastFrame) / (float)lastFrame)
                : inX;

            if (outX > inX)
            {
                Draw.Rect(inX, TrackY, outX - inX, TrackHeight, Color.LimeGreen);
            }

            RenderMarker(inX);

            if (PlaybackControls.MarkOut.HasValue)
            {
                RenderMarker(outX);
            }
        }

        private static void RenderMarker(float x)
        {
            Draw.Rect(x - 1f, TrackY - 4f, 3f, TrackHeight + 8f, Color.Black * 0.75f);
            Draw.Rect(x, TrackY - 3f, 1f, TrackHeight + 6f, Color.LimeGreen);
        }

        private static void RenderStatus()
        {
            if (GifExport.State == GifExport.Phase.Idle)
            {
                return;
            }

            var color = GifExport.State switch
            {
                GifExport.Phase.Failed => Color.OrangeRed,
                GifExport.Phase.Done => Color.LimeGreen,
                _ => Color.White,
            };

            var text = GifExport.IsBusy
                ? $"{GifExport.Message} {(int)(GifExport.Progress * 100f)}%"
                : GifExport.Message;

            Draw.OutlineTextCentered(TFGame.Font, text ?? "", new Vector2(CenterX, StatusY), color, Color.Black);
        }
    }
}
