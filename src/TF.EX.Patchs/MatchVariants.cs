using System.Globalization;

namespace TF.EX.Patchs
{
    internal class MatchVariantsPatchs : IHookable
    {
        private List<string> UnauthorizedVariant = new List<string>
        {
            "SuddenDeath",
            "TeamRevive",
            "AlwaysBigTreasure",
            "BottomlessTreasure",
            "StartWithBombArrows",
            "StartWithLaserArrows",
            "StartWithBrambleArrows",
            "StartWithDrillArrows",
            "StartWithBoltArrows",
            "StartWithSuperBombArrows",
            "StartWithFeatherArrows",
            "StartWithTriggerArrows",
            "StartWithPrismArrows",
            "StartWithRandomArrows",
            "StartWithToyArrows",
            "InfiniteLasers",
            "InfiniteDrills",
            "InfiniteBrambles",
            "RegeneratingShields",
            "RegeneratingArrows",
            "StealthArchers",
            "DoubleJumping",
            "ExplodingCorpses",
            "TriggerCorpses",
            "ReturnAsGhosts",
            "CorpsesDropArrows",
            "Encumbrance",
            "SmallQuivers",
            "NoQuivers",
            "FreeAiming",
            "NoSlipping",
            "SlipperyFloors",
            "OffsetWorld",
            "DarkPortals",
            "ArrowShuffle",
            "ClumsyArchers",
            "TreasureDraft",
            "ShowTreasureSpawns"
        };


        public MatchVariantsPatchs()
        {
            UnauthorizedVariant = UnauthorizedVariant.Select(GetVariantTitle).ToList();
        }

        public void Load()
        {
            On.TowerFall.MatchVariants.ctor += MatchVariants_ctor;
        }

        public void Unload()
        {
            On.TowerFall.MatchVariants.ctor -= MatchVariants_ctor;
        }

        private void MatchVariants_ctor(On.TowerFall.MatchVariants.orig_ctor orig, TowerFall.MatchVariants self, bool noPerPlayer)
        {
            orig(self, noPerPlayer);

            self.Variants = self.Variants.Where(v => !UnauthorizedVariant.Contains(v.Title)).ToArray();
            self.TournamentRules();
        }


        private string GetVariantTitle(string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                {
                    text = text.Substring(0, i) + " " + text.Substring(i);
                    i++;
                }
            }

            return text.ToUpper(CultureInfo.InvariantCulture);
        }
    }
}
