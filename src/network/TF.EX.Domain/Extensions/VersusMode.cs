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

        public static bool ApplyNetplayMode(this TowerFall.MatchSettings settings)
        {
            if (settings == null || !FortRise.GameModeRegistry.ModesToVersusGameMode.TryGetValue(NetplayMode, out var entry))
            {
                return false;
            }

            settings.Mode = entry.Modes;
            settings.IsCustom = true;

            MonoMod.Utils.DynamicData.For(settings).Set("CustomVersusModeName", entry.Name);

            return true;
        }

        public static void ClearNetplayMode(this TowerFall.MatchSettings settings)
        {
            if (settings == null || !settings.Mode.IsNetplay())
            {
                return;
            }

            settings.Mode = TowerFall.Modes.LastManStanding;
            settings.IsCustom = false;

            MonoMod.Utils.DynamicData.For(settings).Set("CustomVersusModeName", null);
        }
    }
}
