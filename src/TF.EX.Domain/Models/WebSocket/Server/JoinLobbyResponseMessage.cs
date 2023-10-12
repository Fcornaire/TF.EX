using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    internal class JoinLobbyResponseMessage
    {

        [JsonPropertyName("JoinLobbyResponse")]
        public JoinLobbyResponse JoinLobbyResponse { get; set; } = new JoinLobbyResponse();
    }

    public class JoinLobbyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("room_peer_id")]
        public string RoomPeerId { get; set; }

        [JsonPropertyName("room_chat_peer_id")]
        public string RoomChatPeerId { get; set; }
    }
}
