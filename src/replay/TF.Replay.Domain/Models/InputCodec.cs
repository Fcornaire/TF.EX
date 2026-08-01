namespace TF.Replay.Domain.Models
{
    public static class InputCodec
    {
        public const int JumpCheck = 0;
        public const int JumpPressed = 1;
        public const int ShootCheck = 2;
        public const int ShootPressed = 3;
        public const int AltShootCheck = 4;
        public const int AltShootPressed = 5;
        public const int DodgeCheck = 6;
        public const int DodgePressed = 7;
        public const int ArrowPressed = 8;
        public const int MoveX = 9;
        public const int MoveY = 10;
        public const int AimX = 11;
        public const int AimY = 12;
        public const int AimRightX = 13;
        public const int AimRightY = 14;
        public const int Disconnected = 15;

        public const int Stride = 16;

        public static int FromFloat(float value) => BitConverter.SingleToInt32Bits(value);

        public static float ToFloat(int bits) => BitConverter.Int32BitsToSingle(bits);

        public static int SeatCount(int[] flat) => flat == null ? 0 : flat.Length / Stride;

        public static int Offset(int seat) => seat * Stride;
    }
}
