using TowerFall;

namespace TF.State.Domain.Context
{
    public static class ResultsSaveStats
    {
        private static bool _hasCounted;
        private static ulong[] _wins;
        private static ulong[][] _awards;
        private static int _versusTipMatches;
        private static int[] _archerPlays;
        private static int[] _archerWins;

        public static void OnlyCountOnce()
        {
            var stats = SaveData.Instance?.Stats;

            if (stats == null)
            {
                return;
            }

            if (!_hasCounted)
            {
                _wins = (ulong[])stats.Wins?.Clone();
                _awards = stats.Awards?.Select(award => (ulong[])award?.Clone()).ToArray();
                _versusTipMatches = stats.VersusTipMatches;
                _archerPlays = (int[])SessionStats.ArcherPlays?.Clone();
                _archerWins = (int[])SessionStats.ArcherWins?.Clone();
                _hasCounted = true;
                return;
            }

            CopyInto(_wins, stats.Wins);

            for (int i = 0; _awards != null && stats.Awards != null && i < _awards.Length && i < stats.Awards.Length; i++)
            {
                CopyInto(_awards[i], stats.Awards[i]);
            }

            stats.VersusTipMatches = _versusTipMatches;
            CopyInto(_archerPlays, SessionStats.ArcherPlays);
            CopyInto(_archerWins, SessionStats.ArcherWins);
        }

        public static void Reset()
        {
            _hasCounted = false;
            _wins = null;
            _awards = null;
            _versusTipMatches = 0;
            _archerPlays = null;
            _archerWins = null;
        }

        private static void CopyInto<T>(T[] from, T[] to)
        {
            if (from != null && to != null)
            {
                Array.Copy(from, to, Math.Min(from.Length, to.Length));
            }
        }
    }
}
