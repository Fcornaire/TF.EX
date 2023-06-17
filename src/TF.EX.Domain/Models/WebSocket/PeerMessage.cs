using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket
{
    public class PeerMessage
    {
        [JsonProperty("peer_id")]
        public Guid PeerId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("peer_message_type")]
        public PeerMessageType Type { get; set; }
    }

    public enum PeerMessageType
    {
        Ping,
        Pong,
        Archer,
        Greetings,
    }
}
