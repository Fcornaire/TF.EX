using Monocle;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain
{
    public static class VersusLevelSystemExtensions
    {
        public static List<string> OwnGenLevel(
            this VersusLevelSystem versusLevelSystem,
            MatchSettings matchSettings,
            VersusTowerData versusTowerData,
            string lastLevel,
            IRngService rngService
            )
        {
            var levels = versusTowerData.GetLevels(matchSettings);

            //useful for debug only
            // for (int i = 0; i < levels.Count; i++)
            // {
            //     levels[i] = $"Content\\Levels\\Versus\\01 - Twilight Spire\\04.oel";
            // }

            if (versusTowerData.FixedFirst && lastLevel == null)
            {
                string item = levels[0];
                levels.RemoveAt(0);
                Calc.Shuffle(levels, new Random(rngService.GetSeed()));
                levels.Insert(0, item);
                return levels;
            }

            Calc.Shuffle(levels, new Random(rngService.GetSeed()));
            if (levels[0] == lastLevel)
            {
                levels.RemoveAt(0);
                levels.Add(lastLevel);
            }

            return levels;
        }
    }
}
