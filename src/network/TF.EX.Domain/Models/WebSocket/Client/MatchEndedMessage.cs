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
    }
}
