namespace TF.EX.Domain.Models
{
    public static class Constants
    {
        public const string DRIVER_NAME = "TF.EX";

        public static readonly IEnumerable<string> NETPLAY_SAFE_MAP = new List<string> //Not all map are supported in netplay right now... //TODO: it actually is, remove this next time
        {
            "SACRED GROUND",
            "TWILIGHT SPIRE",
            "BACKFIRE",
            "FLIGHT",
            "MIRAGE",
            "THORNWOOD",
            "FROSTFANG KEEP",
            "KING'S COURT",
            "SUNKEN CITY",
            "MOONSTONE",
            "TOWERFORGE",
            "ASCENSION",
            "THE AMARANTH",
            "DREADWOOD",
            "DARKFANG",
            "CATACLYSM"
        };

        public const string RIGHT_STICK_VARIANT_NAME = "RightStickShot";
        public const string RIGHT_STICK_VARIANT_FULLNAME = "TF.EX/RightStickShot";
        public const string RIGHT_STICK_VARIANT_TITLE = "RIGHT STICK SHOT";

        public const string NETPLAY_INPUT_DELAY_TITLE = "NETPLAY INPUT DELAY";
        public const string NETPLAY_USERNAME_TITLE = "NETPLAY USERNAME";
    }
}
