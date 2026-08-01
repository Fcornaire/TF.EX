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
                    found.Value = true;
                    continue;
                }

                var custom = matchVariants.CustomVariants.FirstOrDefault(v => v.Value.Title == variant);

                if (custom.Value != null)
                {
                    custom.Value.Value = true;
                    continue;
                }

                unknown.Add(variant);
            }

            return unknown;
        }
    }
}
