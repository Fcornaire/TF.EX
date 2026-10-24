using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class MatchEndedMessage
    {
        [DataMember(Name = "MatchEnded")]
        public MatchEnded MatchEnded { get; set; } = new MatchEnded();
    }

    [DataContract]
    public class MatchEnded
    {
        [DataMember(Name = "winner_seat")]
        public int? WinnerSeat { get; set; }

        [DataMember(Name = "frame")]
        public int? Frame { get; set; }

        [DataMember(Name = "checksum")]
        public string Checksum { get; set; }

        [DataMember(Name = "scores")]
        public List<int> Scores { get; set; }

        [DataMember(Name = "stats")]
        public List<SeatStats> Stats { get; set; }

        [DataMember(Name = "tower")]
        public int? Tower { get; set; }
    }

    [DataContract]
    public class SeatStats
    {
        [DataMember(Name = "seat")]
        public int Seat { get; set; }

        [DataMember(Name = "archer")]
        public int Archer { get; set; }

        [DataMember(Name = "archer_alt")]
        public int ArcherAlt { get; set; }

        [DataMember(Name = "custom_archer_id")]
        public string CustomArcherId { get; set; } = "";

        [DataMember(Name = "awards")]
        public List<int> Awards { get; set; } = [];

        [DataMember(Name = "raw_stats")]
        public Dictionary<string, double> RawStats { get; set; } = [];
    }
}
