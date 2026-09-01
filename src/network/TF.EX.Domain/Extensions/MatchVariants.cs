using Monocle;
using TowerFall;
using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class MatchVariantsExtensions
    {
        public static void ApplyVariants(this MatchVariants matchVariants, IEnumerable<string> variants)
        {
            matchVariants.DisableAll();
            foreach (var variant in variants)
            {
                var notFound = true;
                var varian = matchVariants.Variants.FirstOrDefault(v => v.Title == variant);
                if (varian != null)
                {
                    varian.EnableForAll();
                    notFound = false;
                }
                else
                {
                    var variantCustom = matchVariants.CustomVariants.FirstOrDefault(v => v.Value.Title == variant);
                    if (variantCustom.Value != null)
                    {
                        variantCustom.Value.EnableForAll();
                        notFound = false;
                    }
                }

                if (notFound)
                {
                    //FortRise.Logger.Log($"Variant {variant} not found");
                }
            }
        }

        public static bool ContainsCustomVariant(this MatchVariants matchVariants, IEnumerable<string> variants)
        {
            return variants.Any(variant => variant != Constants.RIGHT_STICK_VARIANT_TITLE && matchVariants.CustomVariants.Any(v => v.Value.Title == variant));
        }

        public static List<string> MissingVariants(this MatchVariants matchVariants, IEnumerable<string> variants)
        {
            return variants
                .Where(title => matchVariants.Variants.All(v => v.Title != title)
                    && matchVariants.CustomVariants.All(pair => pair.Value.Title != title))
                .ToList();
        }

        public static List<string> CustomVariantTitles(this MatchVariants matchVariants)
        {
            return matchVariants.CustomVariants
                .Where(pair => pair.Key.Contains('/'))
                .Select(pair => pair.Value.Title)
                .ToList();
        }

        public static Subtexture FindVariantIcon(this MatchVariants matchVariants, string title)
        {
            var variant = matchVariants.Variants.FirstOrDefault(v => v.Title == title) ?? matchVariants.CustomVariants.Values.FirstOrDefault(v => v.Title == title);

            return variant?.Icon;
        }

        public static void EnableForAll(this Variant variant)
        {
            if (variant.PerPlayer)
            {
                var dynVariant = MonoMod.Utils.DynamicData.For(variant);
                var values = dynVariant.Get<bool[]>("playerValues");

                if (values == null)
                {
                    values = new bool[4];
                    dynVariant.Set("playerValues", values);
                }

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = true;
                }

                return;
            }

            variant.Value = true;
        }
    }
}
