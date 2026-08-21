using TowerFall;

namespace TF.Replay.Domain.Extensions
{
    public static class MatchVariantsExtensions
    {
        public static List<string> ApplyVariants(this MatchVariants matchVariants, IEnumerable<string> variants)
        {
            matchVariants.DisableAll();

            var unknown = new List<string>();

            foreach (var variant in variants)
            {
                var found = matchVariants.Variants.FirstOrDefault(v => v.Title == variant);

                if (found != null)
                {
                    found.EnableForAll();
                    continue;
                }

                var custom = matchVariants.CustomVariants.FirstOrDefault(v => v.Value.Title == variant);

                if (custom.Value != null)
                {
                    custom.Value.EnableForAll();
                    continue;
                }

                unknown.Add(variant);
            }

            return unknown;
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
