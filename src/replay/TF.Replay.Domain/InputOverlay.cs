using TF.Replay.Domain.Models;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class InputOverlay
    {
        private static InputRenderer[] _renderers;

        public static void Render(int[] flat)
        {
            var seats = InputCodec.SeatCount(flat);

            if (seats == 0)
            {
                return;
            }

            if (_renderers == null)
            {
                Setup(seats);

                if (_renderers == null)
                {
                    return;
                }
            }

            for (int seat = 0; seat < _renderers.Length && seat < seats; seat++)
            {
                _renderers[seat]?.Render(PlaybackInputs.Decode(flat, seat));
            }
        }

        public static void Reset() => _renderers = null;

        private static void Setup(int seats)
        {
            var renderers = new InputRenderer[seats];

            float width = 0f;
            var created = 0;

            for (int seat = 0; seat < seats; seat++)
            {
                if (seat >= TFGame.Players.Length || !TFGame.Players[seat])
                {
                    continue;
                }

                if (seat >= TFGame.PlayerInputs.Length || TFGame.PlayerInputs[seat] == null)
                {
                    continue;
                }

                renderers[seat] = new InputRenderer(seat, width);
                width += renderers[seat].Width;
                created++;
            }

            _renderers = created > 0 ? renderers : null;
        }
    }
}
