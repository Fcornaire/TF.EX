namespace TF.EX.Domain.Extensions
{
    public static class VersusModeExtensions
    {
        public const int NetplayModeValue = 11; //fallback ,this is only true if no other mode registered

        private static TowerFall.Modes? registeredNetplayMode;

        public static TowerFall.Modes NetplayMode => registeredNetplayMode ?? (TowerFall.Modes)NetplayModeValue;

        public static void SetNetplayMode(TowerFall.Modes mode) => registeredNetplayMode = mode;

        public static bool IsNetplay(this TowerFall.Modes mode) => mode == NetplayMode;

        public static bool ApplyNetplayMode(this TowerFall.MatchSettings settings, bool applyVariantRules = true)
        {
            if (settings == null || !FortRise.GameModeRegistry.ModesToVersusGameMode.TryGetValue(NetplayMode, out var entry))
            {
                return false;
            }

            settings.Mode = entry.Modes;
            settings.IsCustom = true;

            MonoMod.Utils.DynamicData.For(settings).Set("CustomVersusModeName", entry.Name);

            if (applyVariantRules)
            {
                settings.Variants.ApplyNetplayVariantRules();
            }

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

            settings.Variants.DisableAll();
            RestorePerPlayerVariants(settings.Variants);
        }

        private static readonly Dictionary<TowerFall.Variant, bool[]> netplayNormalizedVariants = [];

        public static void NormalizeForNetplay(this TowerFall.MatchVariants variants)
        {
            foreach (var variant in variants.Variants)
            {
                if (variant.PerPlayer)
                {
                    var dynVariant = MonoMod.Utils.DynamicData.For(variant);

                    netplayNormalizedVariants[variant] = dynVariant.Get<bool[]>("playerValues");

                    dynVariant.Set("playerValues", null);
                    dynVariant.Set("value", false);
                }
            }
        }

        public static void ApplyNetplayVariantRules(this TowerFall.MatchVariants variants)
        {
            variants.NormalizeForNetplay();

            variants.TournamentRules();
        }

        private static void RestorePerPlayerVariants(TowerFall.MatchVariants variants)
        {
            foreach (var variant in variants.Variants)
            {
                if (netplayNormalizedVariants.Remove(variant, out var playerValues) && playerValues != null)
                {
                    Array.Clear(playerValues);
                    MonoMod.Utils.DynamicData.For(variant).Set("playerValues", playerValues);
                }
            }
        }
    }
}
