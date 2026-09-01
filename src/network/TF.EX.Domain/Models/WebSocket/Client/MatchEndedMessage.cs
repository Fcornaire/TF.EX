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
    }
}
