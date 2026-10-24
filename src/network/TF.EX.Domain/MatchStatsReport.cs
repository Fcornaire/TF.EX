using System.Reflection;
using TF.EX.Domain.Models.WebSocket.Client;

namespace TF.EX.Domain
{
    public static class MatchStatsReport
    {
        private static readonly Dictionary<int, List<int>> _awards = [];
        private static List<SeatStats> _stats;
        private static int? _tower;

        public static void RecordAwards(int seat, IEnumerable<TowerFall.AwardInfo> awards)
        {
            _awards[seat] = awards?.Select(award => award.ID).ToList() ?? [];
        }

        public static void Save(TowerFall.Level level)
        {
            var matchStats = level?.Session?.MatchStats;

            _tower = (level?.Session?.MatchSettings?.LevelSystem as TowerFall.VersusLevelSystem)?.ID.X;

            _stats = matchStats == null
                ? null
                : [.. ServiceCollections.ResolveNetplayManager().GetArchersInfo()
                    .Where(archer => archer.Seat >= 0 && archer.Seat < matchStats.Length)
                    .Select(archer => new SeatStats
                    {
                        Seat = archer.Seat,
                        Archer = archer.Index,
                        ArcherAlt = archer.Type,
                        CustomArcherId = archer.CustomArcherId ?? "",
                        Awards = _awards.TryGetValue(archer.Seat, out var awards) ? awards : [],
                        RawStats = BuildRawStats(matchStats[archer.Seat]),
                    })];

            _awards.Clear();
        }

        public static (List<SeatStats> Stats, int? Tower) Take()
        {
            var captured = (_stats, _tower);
            _stats = null;
            _tower = null;

            return captured;
        }

        private static Dictionary<string, double> BuildRawStats(TowerFall.MatchStats stats)
        {
            var rawStats = new Dictionary<string, double>();
            object copy = stats;

            foreach (var field in typeof(TowerFall.MatchStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(copy);

                if (value is TowerFall.KillStats)
                {
                    foreach (var killField in typeof(TowerFall.KillStats).GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        rawStats[$"{field.Name}.{killField.Name}"] = Convert.ToDouble(killField.GetValue(value));
                    }
                }
                else
                {
                    rawStats[field.Name] = Convert.ToDouble(value);
                }
            }

            return rawStats;
        }
    }
}
