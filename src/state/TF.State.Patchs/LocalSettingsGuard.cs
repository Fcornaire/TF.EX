using TF.State.Domain.Context;
using TF.State.Domain.Extensions;
using TowerFall;

namespace TF.State.Patchs
{
    // Loot tables and orb effects read this machine's save/options; every peer must see the same values
    public static class LocalSettingsGuard
    {
        public readonly struct Snapshot(bool applied, bool removeScrollEffects, bool sunkenCity, bool towerForge, bool ascension, bool noDarkWorldOverride)
        {
            public readonly bool Applied = applied;
            public readonly bool RemoveScrollEffects = removeScrollEffects;
            public readonly bool SunkenCity = sunkenCity;
            public readonly bool TowerForge = towerForge;
            public readonly bool Ascension = ascension;
            public readonly bool NoDarkWorldOverride = noDarkWorldOverride;
        }

        public static Snapshot Neutralize()
        {
            var save = SaveData.Instance;

            if (save == null || !IsExSession())
            {
                return default;
            }

            var snapshot = new Snapshot(
                true,
                save.Options.RemoveScrollEffects,
                save.Unlocks.SunkenCity,
                save.Unlocks.TowerForge,
                save.Unlocks.Ascension,
                GameData.NoDarkWorldOverride);

            save.Options.RemoveScrollEffects = false;
            save.Unlocks.SunkenCity = true;
            save.Unlocks.TowerForge = true;
            save.Unlocks.Ascension = true;
            GameData.NoDarkWorldOverride = false;

            return snapshot;
        }

        private static bool IsExSession()
        {
            if (StateFlags.IsTestMode || StateFlags.IsReplayMode)
            {
                return true;
            }

            var settings = TowerFall.MainMenu.VersusMatchSettings;

            return settings != null && settings.Mode.ToModel().IsNetplay();
        }

        public static void Restore(Snapshot snapshot)
        {
            if (!snapshot.Applied)
            {
                return;
            }

            var save = SaveData.Instance;

            save.Options.RemoveScrollEffects = snapshot.RemoveScrollEffects;
            save.Unlocks.SunkenCity = snapshot.SunkenCity;
            save.Unlocks.TowerForge = snapshot.TowerForge;
            save.Unlocks.Ascension = snapshot.Ascension;
            GameData.NoDarkWorldOverride = snapshot.NoDarkWorldOverride;
        }
    }
}
