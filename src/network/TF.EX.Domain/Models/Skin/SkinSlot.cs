namespace TF.EX.Domain.Models.Skin
{

    public static class SkinSlot
    {
        public static int? CurrentSeat { get; private set; }

        public static void Enter(int seat)
        {
            CurrentSeat = seat;
        }

        public static void Exit()
        {
            CurrentSeat = null;
        }
    }
}
