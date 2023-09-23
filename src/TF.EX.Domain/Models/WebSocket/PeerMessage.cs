using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket
{
    public class PeerMessage
    {
        [JsonPropertyName("peer_id")]
        public Guid PeerId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("peer_message_type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
