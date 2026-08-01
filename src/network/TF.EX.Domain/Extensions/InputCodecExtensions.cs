using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    /// <summary>
    /// Converts EX's FFI <see cref="Input"/> struct to and from the flat seat-major int[] that TF.Replay stores,since struct is [StructLayout(Sequential)] and marshalled to GGRS ,it can never cross a mod boundary
    /// </summary>
    public static class InputCodecExtensions
    {
        public static int[] ToFlatInputs(this IEnumerable<Input> inputs)
        {
            var list = inputs?.ToList() ?? new List<Input>();
            var flat = new int[list.Count * InputCodec.Stride];

            for (int seat = 0; seat < list.Count; seat++)
            {
                var input = list[seat];
                var at = InputCodec.Offset(seat);

                flat[at + InputCodec.JumpCheck] = input.jump_check;
                flat[at + InputCodec.JumpPressed] = input.jump_pressed;
                flat[at + InputCodec.ShootCheck] = input.shoot_check;
                flat[at + InputCodec.ShootPressed] = input.shoot_pressed;
                flat[at + InputCodec.AltShootCheck] = input.alt_shoot_check;
                flat[at + InputCodec.AltShootPressed] = input.alt_shoot_pressed;
                flat[at + InputCodec.DodgeCheck] = input.dodge_check;
                flat[at + InputCodec.DodgePressed] = input.dodge_pressed;
                flat[at + InputCodec.ArrowPressed] = input.arrow_pressed;
                flat[at + InputCodec.MoveX] = input.move_x;
                flat[at + InputCodec.MoveY] = input.move_y;
                flat[at + InputCodec.AimX] = InputCodec.FromFloat(input.aim_axis.X);
                flat[at + InputCodec.AimY] = InputCodec.FromFloat(input.aim_axis.Y);
                flat[at + InputCodec.AimRightX] = InputCodec.FromFloat(input.aim_right_axis.X);
                flat[at + InputCodec.AimRightY] = InputCodec.FromFloat(input.aim_right_axis.Y);
                flat[at + InputCodec.Disconnected] = input.disconnected;
            }

            return flat;
        }

        public static List<Input> ToInputs(this int[] flat)
        {
            var seats = InputCodec.SeatCount(flat);
            var inputs = new List<Input>(seats);

            for (int seat = 0; seat < seats; seat++)
            {
                var at = InputCodec.Offset(seat);

                inputs.Add(new Input
                {
                    jump_check = flat[at + InputCodec.JumpCheck],
                    jump_pressed = flat[at + InputCodec.JumpPressed],
                    shoot_check = flat[at + InputCodec.ShootCheck],
                    shoot_pressed = flat[at + InputCodec.ShootPressed],
                    alt_shoot_check = flat[at + InputCodec.AltShootCheck],
                    alt_shoot_pressed = flat[at + InputCodec.AltShootPressed],
                    dodge_check = flat[at + InputCodec.DodgeCheck],
                    dodge_pressed = flat[at + InputCodec.DodgePressed],
                    arrow_pressed = flat[at + InputCodec.ArrowPressed],
                    move_x = flat[at + InputCodec.MoveX],
                    move_y = flat[at + InputCodec.MoveY],
                    aim_axis = new Vector2f
                    {
                        X = InputCodec.ToFloat(flat[at + InputCodec.AimX]),
                        Y = InputCodec.ToFloat(flat[at + InputCodec.AimY]),
                    },
                    aim_right_axis = new Vector2f
                    {
                        X = InputCodec.ToFloat(flat[at + InputCodec.AimRightX]),
                        Y = InputCodec.ToFloat(flat[at + InputCodec.AimRightY]),
                    },
                    disconnected = flat[at + InputCodec.Disconnected],
                });
            }

            return inputs;
        }
    }
}
