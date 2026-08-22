using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class TakeoverOverlay
    {
        private static float CenterX => 160f + Overlay.CenterOffset;

        public static void Render()
        {
            switch (Takeover.State)
            {
                case Takeover.Phase.Countdown:
                    Draw.OutlineTextCentered(TFGame.Font, 
                    $"P{Takeover.Seat + 1} TAKEOVER IN {Takeover.CountdownSecondsLeft}",
                    new Vector2(CenterX, 110f), 
                    SeatPicker.SeatColor(Takeover.Seat), 
                    Color.Black, 
                    2f);
                    break;

                case Takeover.Phase.Active:
                    Draw.OutlineTextCentered(TFGame.Font,
                     $"TAKEOVER P{Takeover.Seat + 1}",
                     new Vector2(CenterX, 44f), 
                     SeatPicker.SeatColor(Takeover.Seat), 
                     Color.Black);

                    break;
            }
        }
    }
}
