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
        public const int MaxInputDelay = 332;
        public const int InputDelayStep = 4;
        public const int MaxNameLength = 10;
        public const string OfficialServer = "wss://tfex-server.balatro-vs-matchmaking.eu";
        public const string LocalServer = "ws://127.0.0.1:3000";

        public static int InputDelay = 20;
        public static string Name = "PLAYER";
        public static string PlayerId = "";
        public static string Server = OfficialServer;
        public static AutoAdjustInputDelayMode AutoAdjustInputDelay = AutoAdjustInputDelayMode.Propose;
        public static CustomSkinMode CustomSkins = CustomSkinMode.Full;
        public static bool AutoUpdate = true;

        public static bool IsOfficialServer => IsOfficial(Server);

        public static int InputDelayFrames => ToFrames(InputDelay);

        public static int ToFrames(int ms) => (ms * Constants.NETPLAY_FPS + 999) / 1000;

        public static bool IsOfficial(string server)
        {
            return server.Equals(OfficialServer);
        }
    }
}
