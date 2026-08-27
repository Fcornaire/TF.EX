using TowerFall;

namespace TF.Replay.Domain.Extensions
{
    public static class ArcherDataExtensions
    {
        public const int VanillaArcherCount = 9;

        public static bool Exists(int archerIndex, int altIndex)
        {
            if (archerIndex < 0 || ArcherData.Archers == null || archerIndex >= ArcherData.Archers.Length)
            {
                return false;
            }

            try
            {
                return ArcherData.Get(archerIndex, (ArcherData.ArcherTypes)altIndex) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetCustomArcherId(int archerIndex, int altIndex)
        {
            if (archerIndex < VanillaArcherCount)
            {
                return "";
            }

            try
            {
                var entries = Interop.ArcherRegistryApi.Current?.GetAllArchers();

                if (entries == null)
                {
                    return "";
                }

                var entry = entries.FirstOrDefault(e => e?.Index == archerIndex && (int)e.Type == altIndex)
                    ?? entries.FirstOrDefault(e => e?.Index == archerIndex && e.Type == FortRise.ArcherEntryType.Normal)
                    ?? entries.FirstOrDefault(e => e?.Index == archerIndex);

                return entry?.Name ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static int ResolveCustomArcher(string customArcherId)
        {
            if (string.IsNullOrEmpty(customArcherId))
            {
                return -1;
            }

            try
            {
                var registered = Interop.ArcherRegistryApi.Current?.RegisteredArchers;

                return registered != null && registered.TryGetValue(customArcherId, out var entry)
                    ? entry?.Index ?? -1
                    : -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static (int, int) EnsureArcherDataExist(int archerIndex, int altIndex, ISet<int> taken)
        {
            if (Exists(archerIndex, altIndex))
            {
                taken.Add(archerIndex);

                return (archerIndex, altIndex);
            }

            for (int index = 0; index < VanillaArcherCount; index++)
            {
                if (taken.Contains(index))
                {
                    continue;
                }

                if (Exists(index, altIndex))
                {
                    taken.Add(index);

                    return (index, altIndex);
                }

                for (int alt = 0; alt <= (int)ArcherData.ArcherTypes.Secret; alt++)
                {
                    if (!Exists(index, alt))
                    {
                        continue;
                    }

                    taken.Add(index);

                    return (index, alt);
                }
            }

            return (archerIndex, altIndex);
        }
    }
}
