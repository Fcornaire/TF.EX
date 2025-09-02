using HarmonyLib;
using System.Globalization;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(MatchVariants))]
    internal class MatchVariantsPatchs
    {
        private static List<string> UnauthorizedVariant =
        [
            "SuddenDeath",
            "TeamRevive",
            "AlwaysBigTreasure",
            "BottomlessTreasure",
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
            "NoSlipping",
            "SlipperyFloors",
            "OffsetWorld",
            "DarkPortals",
            "ArrowShuffle",
            "ClumsyArchers",
            "TreasureDraft",
            "ShowTreasureSpawns"
        ];

        private static bool hasInit = false;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(bool)])]
        public static void MatchVariants_ctor(MatchVariants __instance)
        {
            if (!hasInit)
            {
                UnauthorizedVariant = UnauthorizedVariant.Select(GetVariantTitle).ToList();
                hasInit = true;
            }

            __instance.Variants = __instance.Variants.Where(v => !UnauthorizedVariant.Contains(v.Title)).ToArray();
            __instance.TournamentRules();
            __instance.Variants.First(variant => variant.Title == "FREE AIMING").Value = true;
            if (__instance.CustomVariants.ContainsKey(Constants.RIGHT_STICK_VARIANT_NAME))
            {
                __instance.CustomVariants.TryGetValue(Constants.RIGHT_STICK_VARIANT_NAME, out var variant);
                variant.Value = true;
            }
        }


        private static string GetVariantTitle(string text)
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
