namespace TF.EX.Domain.Extensions
{
    public static class VersusModeExtensions
    {
        public const string NetplayModeName = "NETPLAY";

        public const int NetplayModeValue = 11;

        public static TowerFall.Modes NetplayMode => (TowerFall.Modes)NetplayModeValue;

        public static bool IsNetplay(this TowerFall.Modes mode)
            => FortRise.GameModeRegistry.ModesToVersusGameMode.TryGetValue(mode, out var entry)
               && entry.VersusGameMode?.Name?.ToUpperInvariant() == NetplayModeName;
    }
}
