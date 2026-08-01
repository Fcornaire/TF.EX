using HarmonyLib;
using TF.EX.Domain.Models;
using System.Globalization;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(MatchVariants))]
    internal class MatchVariantsPatchs
    {
        private static List<string> UnauthorizedVariant =
        [
            //TODO: need to finish
            "TeamRevive",
            //TODO: would need some work
            "TreasureDraft"
        ];

        private static bool hasInit = false;

        private static readonly Dictionary<Variant, bool> hiddenBeforeNetplay = new Dictionary<Variant, bool>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(MatchVariants.BuildMenu))]
        public static void MatchVariants_BuildMenu_Prefix(MatchVariants __instance)
        {
            var isNetplay = TowerFall.MainMenu.VersusMatchSettings != null && TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay();

            foreach (var variant in __instance.Variants)
            {
                if (!UnauthorizedVariant.Contains(variant.Title))
                {
                    continue;
                }

                if (isNetplay)
                {
                    if (!hiddenBeforeNetplay.ContainsKey(variant))
                    {
                        hiddenBeforeNetplay[variant] = variant.Hidden;
                    }

                    variant.Hidden = true;
                }
                else if (hiddenBeforeNetplay.TryGetValue(variant, out var hidden))
                {
                    variant.Hidden = hidden;
                }
            }
        }

        public static void DisableUnauthorized(MatchVariants matchVariants)
        {
            foreach (var variant in matchVariants.Variants)
            {
                if (UnauthorizedVariant.Contains(variant.Title))
                {
                    variant.Value = false;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(bool)])]
        public static void MatchVariants_ctor(MatchVariants __instance)
        {
            if (!hasInit)
            {
                UnauthorizedVariant = UnauthorizedVariant.Select(GetVariantTitle).ToList();
                hasInit = true;
            }

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
