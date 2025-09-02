using TF.EX.Domain.Models.State;
using TowerFall;

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
                    varian.Value = true;
                    notFound = false;
                }
                else
                {
                    var variantCustom = matchVariants.CustomVariants.FirstOrDefault(v => v.Value.Title == variant);
                    if (variantCustom.Value != null)
                    {
                        variantCustom.Value.Value = true;
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
    }
}
