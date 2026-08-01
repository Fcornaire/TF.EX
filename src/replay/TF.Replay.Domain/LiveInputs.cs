using TF.Replay.Domain.Models;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class LiveInputs
    {
        public static int[] Capture()
        {
            var seats = TFGame.PlayerInputs?.Length ?? 0;
            var flat = new int[seats * InputCodec.Stride];

            for (int seat = 0; seat < seats; seat++)
            {
                var input = TFGame.PlayerInputs[seat];

                if (input == null)
                {
                    continue;
                }

                var state = input.GetState();
                var at = InputCodec.Offset(seat);

                flat[at + InputCodec.JumpCheck] = state.JumpCheck ? 1 : 0;
                flat[at + InputCodec.JumpPressed] = state.JumpPressed ? 1 : 0;
                flat[at + InputCodec.ShootCheck] = state.ShootCheck ? 1 : 0;
                flat[at + InputCodec.ShootPressed] = state.ShootPressed ? 1 : 0;
                flat[at + InputCodec.AltShootCheck] = state.AltShootCheck ? 1 : 0;
                flat[at + InputCodec.AltShootPressed] = state.AltShootPressed ? 1 : 0;
                flat[at + InputCodec.DodgeCheck] = state.DodgeCheck ? 1 : 0;
                flat[at + InputCodec.DodgePressed] = state.DodgePressed ? 1 : 0;
                flat[at + InputCodec.ArrowPressed] = state.ArrowsPressed ? 1 : 0;
                flat[at + InputCodec.MoveX] = state.MoveX;
                flat[at + InputCodec.MoveY] = state.MoveY;
                flat[at + InputCodec.AimX] = InputCodec.FromFloat(state.AimAxis.X);
                flat[at + InputCodec.AimY] = InputCodec.FromFloat(state.AimAxis.Y);
            }

            return flat;
        }
    }
}
