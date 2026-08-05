using TF.State.Domain.Extensions;
using TF.State.Domain.Models;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    internal class MatchStatsService : IMatchStatsService
    {
        private readonly Dictionary<int, MatchStats[]> _statsByFrame = new Dictionary<int, MatchStats[]>();

        public void SaveSnapshot(int frame, TowerFall.MatchStats[] stats)
        {
            if (stats == null)
            {
                return;
            }

            _statsByFrame[frame] = stats.Select(stat => stat.ToDomain()).ToArray();

            var cutoff = frame - Constants.SFX_SNAPSHOT_HISTORY;

            foreach (var stale in _statsByFrame.Keys.Where(f => f < cutoff).ToList())
            {
                _statsByFrame.Remove(stale);
            }
        }

        public void RestoreSnapshot(int frame, TowerFall.MatchStats[] stats)
        {
            if (stats == null || !_statsByFrame.TryGetValue(frame, out var snapshot))
            {
                return;
            }

            for (int seat = 0; seat < snapshot.Length && seat < stats.Length; seat++)
            {
                stats[seat] = snapshot[seat].ToTF();
            }
        }

        public void Reset()
        {
            _statsByFrame.Clear();
        }
    }
}
