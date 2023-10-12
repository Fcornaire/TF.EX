using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class ModesExtensions
    {
        public static string ToName(this Modes mode)
        {
            switch (mode)
            {
                case Modes.Netplay:
                    return "Netplay";
                default:
                    return mode.ToString();
            }
        }

        public static Modes ToModel(this TowerFall.Modes mode)
        {
            var modeName = TowerFall.VersusModeButton.GetModeName(mode);

            switch (modeName)
            {
                case "NETPLAY":
                    return Modes.Netplay;
                default:
                    return (Modes)mode;
            }
        }

        public static TowerFall.Modes ToTF(this Modes modes)
        {
            return (TowerFall.Modes)modes;
        }

        public static bool IsNetplay(this Modes mode)
        {
            return mode == Modes.Netplay;
        }
    }
}
