using TowerFall;

namespace TF.Replay.Domain.Extensions
{
    public static class ArcherDataExtensions
    {
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

        public static (int, int) EnsureArcherDataExist(int archerIndex, int altIndex, ISet<int> taken)
        {
            if (Exists(archerIndex, altIndex))
            {
                taken.Add(archerIndex);

                return (archerIndex, altIndex);
            }

            for (int index = 0; index < ArcherData.Archers.Length; index++)
            {
                if (taken.Contains(index))
                {
                    continue;
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
