using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class PlayerProfileMessage
    {
        [DataMember(Name = "PlayerProfile")]
        public PlayerProfileData PlayerProfile { get; set; } = new PlayerProfileData();
    }

    [DataContract]
    public class PlayerProfileData
    {
        [DataMember(Name = "rank")]
        public int? Rank { get; set; }

        [DataMember(Name = "profile")]
        public PlayerProfile Profile { get; set; }
    }

    [DataContract]
    public class PlayerProfile
    {
        [DataMember(Name = "rank")]
        public int? Rank { get; set; }

        [DataMember(Name = "rating")]
        public int? Rating { get; set; }

        [DataMember(Name = "series")]
        public int? Series { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "games")]
        public int Games { get; set; }

        [DataMember(Name = "wins")]
        public int Wins { get; set; }

        [DataMember(Name = "kills")]
        public int Kills { get; set; }

        [DataMember(Name = "deaths")]
        public int Deaths { get; set; }

        [DataMember(Name = "seconds")]
        public double Seconds { get; set; }

        [DataMember(Name = "months")]
        public List<MonthSummary> Months { get; set; } = [];

        [DataMember(Name = "ranked_months")]
        public List<MonthSummary> RankedMonths { get; set; } = [];

        [DataMember(Name = "casual_months")]
        public List<MonthSummary> CasualMonths { get; set; } = [];

        [DataMember(Name = "archers")]
        public List<ArcherPlays> Archers { get; set; } = [];

        [DataMember(Name = "best_towers")]
        public List<TowerPlays> BestTowers { get; set; } = [];

        [DataMember(Name = "awards")]
        public List<AwardCount> Awards { get; set; } = [];

        [DataMember(Name = "stats")]
        public Dictionary<string, StatTotal> Stats { get; set; } = [];
    }

    [DataContract]
    public class MonthSummary
    {
        [DataMember(Name = "month")]
        public int Month { get; set; }

        [DataMember(Name = "games")]
        public int Games { get; set; }

        [DataMember(Name = "wins")]
        public int Wins { get; set; }

        [DataMember(Name = "kills")]
        public int Kills { get; set; }

        [DataMember(Name = "deaths")]
        public int Deaths { get; set; }

        [DataMember(Name = "seconds")]
        public double Seconds { get; set; }
    }

    [DataContract]
    public class ArcherPlays
    {
        [DataMember(Name = "archer")]
        public int Archer { get; set; }

        [DataMember(Name = "archer_alt")]
        public int ArcherAlt { get; set; }

        [DataMember(Name = "custom_archer_id")]
        public string CustomArcherId { get; set; } = "";

        [DataMember(Name = "games")]
        public int Games { get; set; }

        [DataMember(Name = "wins")]
        public int Wins { get; set; }
    }

    [DataContract]
    public class TowerPlays
    {
        [DataMember(Name = "map_id")]
        public int MapId { get; set; }

        [DataMember(Name = "games")]
        public int Games { get; set; }

        [DataMember(Name = "wins")]
        public int Wins { get; set; }
    }

    [DataContract]
    public class AwardCount
    {
        [DataMember(Name = "award")]
        public int Award { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }
    }

    [DataContract]
    public class StatTotal
    {
        [DataMember(Name = "total")]
        public double Total { get; set; }

        [DataMember(Name = "best")]
        public double Best { get; set; }
    }
}
