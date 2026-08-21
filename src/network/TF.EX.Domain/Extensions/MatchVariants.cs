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

        public static void EnableForAll(this Variant variant)
        {
            if (variant.PerPlayer)
            {
                var values = MonoMod.Utils.DynamicData.For(variant).Get<bool[]>("playerValues");
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
