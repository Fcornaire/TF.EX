using HarmonyLib;
using TF.EX.Domain.Models;
using System.Globalization;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(MatchVariants))]
    public class MatchVariantsPatchs
    {
        private static List<string> UnauthorizedVariant =
        [
            //TODO: would need some work
            "TreasureDraft"
        ];

        private static bool hasInit = false;

        private static readonly Dictionary<Variant, bool> hiddenBeforeNetplay = new Dictionary<Variant, bool>();

        private static readonly Dictionary<Variant, bool> valueBeforeNetplay = new Dictionary<Variant, bool>();

        private static readonly HashSet<Variant> restrictedInNetplay = new HashSet<Variant>();

        public static string OwnModName;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(MatchVariants.BuildMenu))]
        public static void MatchVariants_BuildMenu_Prefix(MatchVariants __instance)
        {
            var isNetplay = TowerFall.MainMenu.VersusMatchSettings != null && TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay();

            if (isNetplay)
            {
                __instance.NormalizeForNetplay();
            }

            restrictedInNetplay.Clear();

            foreach (var custom in __instance.CustomVariants)
            {
                if (!custom.Key.Contains('/') || IsOwnVariant(custom.Key) || HasVariantStateEvents(custom.Key))
                {
                    continue;
                }

                if (isNetplay)
                {
                    restrictedInNetplay.Add(custom.Value);
                }

                RestrictInNetplay(custom.Value, isNetplay);
            }

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

        public static bool IsRestricted(Variant variant)
        {
            return variant != null && restrictedInNetplay.Contains(variant);
        }

        private static bool IsOwnVariant(string variantName)
        {
            return !string.IsNullOrEmpty(OwnModName) && variantName.StartsWith(OwnModName + "/", StringComparison.Ordinal);
        }

        private static bool HasVariantStateEvents(string variantName)
        {
            return TF.EX.Domain.Interop.StateApi.Current?.HasStateEvents(variantName) ?? false;
        }

        private static void RestrictInNetplay(Variant variant, bool isNetplay)
        {
            if (isNetplay)
            {
                if (!valueBeforeNetplay.ContainsKey(variant))
                {
                    valueBeforeNetplay[variant] = variant.Value;
                }

                variant.Value = false;
            }
            else if (valueBeforeNetplay.TryGetValue(variant, out var value))
            {
                variant.Value = value;
                valueBeforeNetplay.Remove(variant);
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
