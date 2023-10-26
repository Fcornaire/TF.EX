using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket
{
    [DataContract]
    public class PeerMessage
    {
        [DataMember(Name = "peer_id")]
        public Guid PeerId { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "peer_message_type")]
        public string Type { get; set; }
    }

    public enum PeerMessageType
    {
        Ping,
        Pong,
        Archer,
        Greetings,
    }
}
