using TF.State.Domain.Models;

namespace TF.State.Domain.Extensions
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
            return FortRise.GameModeRegistry.ModesToVersusGameMode.TryGetValue(mode, out var entry)
                   && entry.VersusGameMode?.Name?.ToUpperInvariant() == "NETPLAY"
                ? Modes.Netplay
                : (Modes)mode;
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
