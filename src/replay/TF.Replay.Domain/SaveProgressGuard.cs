using System.Xml.Serialization;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class SaveProgressGuard
    {
        private class SessionSnapshot
        {
            public long TimePlayed;
            public int TotalVersusKills;
            public int MatchesPlayed;
            public int RoundsPlayed;
            public int[] ArcherPlays;
            public int[] ArcherKills;
            public int[] ArcherDeaths;
            public int[] ArcherSelfKills;
            public int[] ArcherWins;
        }

        private static XmlSerializer _serializer;
        private static string _snapshot;
        private static SessionSnapshot _session;

        public static bool IsSuppressing => _snapshot != null && ServiceCollections.ResolveReplayService()?.IsPlayback == true;

        private static XmlSerializer Serializer => _serializer ??= new XmlSerializer(typeof(SaveData));

        public static void Snapshot()
        {
            var save = SaveData.Instance;

            if (save == null)
            {
                return;
            }

            using var writer = new StringWriter();
            Serializer.Serialize(writer, save);

            _session = new SessionSnapshot
            {
                TimePlayed = SessionStats.TimePlayed,
                TotalVersusKills = SessionStats.TotalVersusKills,
                MatchesPlayed = SessionStats.MatchesPlayed,
                RoundsPlayed = SessionStats.RoundsPlayed,
                ArcherPlays = (int[])SessionStats.ArcherPlays?.Clone(),
                ArcherKills = (int[])SessionStats.ArcherKills?.Clone(),
                ArcherDeaths = (int[])SessionStats.ArcherDeaths?.Clone(),
                ArcherSelfKills = (int[])SessionStats.ArcherSelfKills?.Clone(),
                ArcherWins = (int[])SessionStats.ArcherWins?.Clone(),
            };
            _snapshot = writer.ToString();
        }

        public static void Restore()
        {
            var save = SaveData.Instance;
            var snapshot = _snapshot;
            var session = _session;

            _snapshot = null;
            _session = null;

            if (snapshot == null || save == null)
            {
                return;
            }

            using var reader = new StringReader(snapshot);
            var saved = (SaveData)Serializer.Deserialize(reader);

            save.Stats = saved.Stats;
            save.Quest = saved.Quest;
            save.DarkWorld = saved.DarkWorld;
            save.Trials = saved.Trials;
            save.Unlocks = saved.Unlocks;

            SessionStats.TimePlayed = session.TimePlayed;
            SessionStats.TotalVersusKills = session.TotalVersusKills;
            SessionStats.MatchesPlayed = session.MatchesPlayed;
            SessionStats.RoundsPlayed = session.RoundsPlayed;
            SessionStats.ArcherPlays = session.ArcherPlays;
            SessionStats.ArcherKills = session.ArcherKills;
            SessionStats.ArcherDeaths = session.ArcherDeaths;
            SessionStats.ArcherSelfKills = session.ArcherSelfKills;
            SessionStats.ArcherWins = session.ArcherWins;
        }
    }
}
