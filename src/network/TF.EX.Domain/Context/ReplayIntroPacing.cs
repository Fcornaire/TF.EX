namespace TF.EX.Domain.Context
{
    public static class ReplayIntroPacing
    {
        public static bool ConfirmLatched;

        public static void Reset() => ConfirmLatched = false;
    }
}
