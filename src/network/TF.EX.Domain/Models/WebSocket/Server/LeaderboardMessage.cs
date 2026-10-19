using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class LeaderboardMessage
    {
        [DataMember(Name = "Leaderboard")]
        public LeaderboardData Leaderboard { get; set; } = new LeaderboardData();
    }

    [DataContract]
    public class LeaderboardData
    {
        [DataMember(Name = "entries")]
        public List<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();

        [DataMember(Name = "own")]
        public LeaderboardEntry Own { get; set; }

        [DataMember(Name = "around")]
        public List<LeaderboardEntry> Around { get; set; } = new List<LeaderboardEntry>();

        [DataMember(Name = "total")]
        public int Total { get; set; }
    }

    [DataContract]
    public class LeaderboardEntry
    {
        [DataMember(Name = "rank")]
        public int Rank { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "rating")]
        public int Rating { get; set; }

        [DataMember(Name = "matches")]
        public int Matches { get; set; }
    }
}
