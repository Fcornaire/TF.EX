using Microsoft.Xna.Framework;
using Monocle;
using TF.Replay.Domain.Ports;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class SeatPicker
    {
        private const float Y = 229f;
        private const float ArrowHalfWidth = 5f;
        private const float ArrowHalfHeight = 6f;

        private static float RightArrowX => 310f + Overlay.RightOffset;
        private static float LabelX => RightArrowX - 13f;
        private static float LeftArrowX => RightArrowX - 26f;

        public static bool HandleClick(IReplayService service, Vector2 mouse)
        {
            if (!Takeover.PickerVisible(service) || !MInput.Mouse.LeftPressed)
            {
                return false;
            }

            if (Contains(LeftArrowX, mouse))
            {
                Takeover.CycleSeat(-1);
                return true;
            }

            if (Contains(RightArrowX, mouse))
            {
                Takeover.CycleSeat(1);
                return true;
            }

            return false;
        }

        public static void Render(IReplayService service)
        {
            if (!Takeover.PickerVisible(service))
            {
                return;
            }

            var mouse = PlaybackControls.MousePosition;

            Draw.OutlineTextCentered(TFGame.Font, "TAKEOVER", new Vector2(LabelX, Y - 9f), Color.Gold, Color.Black);

            Draw.OutlineTextCentered(TFGame.Font, "<", new Vector2(LeftArrowX, Y),Hovered(LeftArrowX, mouse) ? Color.Gold : Color.White, Color.Black);

            Draw.OutlineTextCentered(TFGame.Font, $"P{Takeover.DisplaySeat + 1}", new Vector2(LabelX, Y),SeatColor(Takeover.DisplaySeat), Color.Black);

            Draw.OutlineTextCentered(TFGame.Font, ">", new Vector2(RightArrowX, Y),Hovered(RightArrowX, mouse) ? Color.Gold : Color.White, Color.Black);
        }

        public static Color SeatColor(int seat)
        {
            try
            {
                return ArcherData.Get(TFGame.Characters[seat], TFGame.AltSelect[seat]).ColorA;
            }
            catch
            {
                return Color.White;
            }
        }

        private static bool Hovered(float x, Vector2? mouse) => mouse.HasValue && Contains(x, mouse.Value);

        private static bool Contains(float x, Vector2 point) => point.X >= x - ArrowHalfWidth && point.X <= x + ArrowHalfWidth
               && point.Y >= Y - ArrowHalfHeight && point.Y <= Y + ArrowHalfHeight;
    }
}
