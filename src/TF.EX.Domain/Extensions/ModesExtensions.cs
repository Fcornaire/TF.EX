using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class ModesExtensions
    {
        public static string ToName(this Modes mode)
        {
            switch (mode)
            {
                case Modes.Netplay1v1QuickPlay:
                    return "Netplay Quickplay";
                case Modes.Netplay1v1Direct:
                    return "Netplay Direct";
                default:
                    return mode.ToString();
            }
        }

        public static Modes ToModel(this TowerFall.Modes mode)
        {
            var modeName = TowerFall.VersusModeButton.GetModeName(mode);

            switch (modeName)
            {
                case "NETPLAY DIRECT":
                    return Modes.Netplay1v1Direct;
                case "NETPLAY QUICKPLAY":
                    return Modes.Netplay1v1QuickPlay;
                default:
                    return (Modes)mode;
            }
        }

        public static TowerFall.Modes ToTF(this Modes modes)
        {
            switch (modes)
            {
                case Modes.Netplay1v1Direct:
                case Modes.Netplay1v1QuickPlay:
                    throw new System.NotImplementedException("TF EX mode cannot be used directly in TowerFall");
                default:
                    return (TowerFall.Modes)modes;
            }
        }

        public static bool IsNetplay(this Modes mode)
        {
            return mode == Modes.Netplay1v1Direct || mode == Modes.Netplay1v1QuickPlay;
        }
    }
}
