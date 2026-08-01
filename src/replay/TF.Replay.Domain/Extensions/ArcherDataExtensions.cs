using Monocle;
using TowerFall;

namespace TF.Replay.Domain.Extensions
{
    public static class ArcherDataExtensions
    {
        public static (int, int) EnsureArcherDataExist(int archerIndex, int altIndex, IEnumerable<int> usedArchers)
        {
            var index = archerIndex;
            var altArcherIndex = altIndex;

            try
            {
                ArcherData.Get(index, (ArcherData.ArcherTypes)altArcherIndex);
            }
            catch (Exception)
            {
                bool hasFound = false;

                while (!hasFound)
                {
                    index = Calc.Random.Next(0, 8);
                    altArcherIndex = Calc.Random.Next(0, 2);

                    if (!usedArchers.Contains(index))
                    {
                        hasFound = true;
                    }
                }
            }

            return (index, altArcherIndex);
        }
    }
}
