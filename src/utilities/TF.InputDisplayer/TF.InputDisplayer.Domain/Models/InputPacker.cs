namespace TF.InputDisplayer.Domain.Models
{
    //Qick bit recap if i ever forgot what this is
    //7 6   5 4   3     2        1      0
    //MoveY MoveX Dodge AltShoot Shoot Jump
    public static class InputPacker
    {
        public const int Jump = 1 << 0;
        public const int Shoot = 1 << 1;
        public const int AltShoot = 1 << 2;
        public const int Dodge = 1 << 3;

        public const int ButtonCount = 4;

        private const int MoveXShift = 4;
        private const int MoveYShift = 6;
        private const int MoveMask = 0x3;

        public static int Pack(int moveX, int moveY, bool jump, bool shoot, bool altShoot, bool dodge)
        {
            var packed = 0;

            if (jump) packed |= Jump;
            if (shoot) packed |= Shoot;
            if (altShoot) packed |= AltShoot;
            if (dodge) packed |= Dodge;

            packed |= (Math.Clamp(moveX, -1, 1) + 1) << MoveXShift;
            packed |= (Math.Clamp(moveY, -1, 1) + 1) << MoveYShift;

            return packed;
        }

        public static int MoveX(int packed) => ((packed >> MoveXShift) & MoveMask) - 1;

        public static int MoveY(int packed) => ((packed >> MoveYShift) & MoveMask) - 1;

        public static bool Held(int packed, int button) => (packed & button) != 0;

        public static int Button(int index) => 1 << index;
    }
}
