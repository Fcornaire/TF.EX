using Microsoft.Xna.Framework;
using TF.Replay.Domain.Models;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class PlaybackInputs
    {
        public static int[] CurrentFlat;

        public static InputState ForSeat(int seat) => Decode(CurrentFlat, seat);

        public static InputState Decode(int[] flat, int seat)
        {
            if (flat == null || seat < 0 || seat >= InputCodec.SeatCount(flat))
            {
                return new InputState();
            }

            var o = InputCodec.Offset(seat);

            return new InputState
            {
                JumpCheck = flat[o + InputCodec.JumpCheck] != 0,
                JumpPressed = flat[o + InputCodec.JumpPressed] != 0,
                ShootCheck = flat[o + InputCodec.ShootCheck] != 0,
                ShootPressed = flat[o + InputCodec.ShootPressed] != 0,
                AltShootCheck = flat[o + InputCodec.AltShootCheck] != 0,
                AltShootPressed = flat[o + InputCodec.AltShootPressed] != 0,
                DodgeCheck = flat[o + InputCodec.DodgeCheck] != 0,
                DodgePressed = flat[o + InputCodec.DodgePressed] != 0,
                ArrowsPressed = flat[o + InputCodec.ArrowPressed] != 0,
                MoveX = flat[o + InputCodec.MoveX],
                MoveY = flat[o + InputCodec.MoveY],
                AimAxis = new Vector2(
                    InputCodec.ToFloat(flat[o + InputCodec.AimX]),
                    InputCodec.ToFloat(flat[o + InputCodec.AimY])),
            };
        }
    }
}
