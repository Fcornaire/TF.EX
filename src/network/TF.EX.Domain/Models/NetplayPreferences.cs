namespace TF.EX.Domain.Models
{
    public enum AutoAdjustInputDelayMode
    {
        Disabled,
        Propose,
        Enabled,
    }

    public enum CustomSkinMode
    {
        Disabled,
        Full,
    }

    public static class NetplayPreferences
    {
        public const int MinInputDelay = 0;
        public const int MaxInputDelay = 20;
        public const int MaxNameLength = 10;

        public static int InputDelay = 2;
        public static string Name = "PLAYER";
        public static AutoAdjustInputDelayMode AutoAdjustInputDelay = AutoAdjustInputDelayMode.Propose;
        public static CustomSkinMode CustomSkins = CustomSkinMode.Full;
    }
}
