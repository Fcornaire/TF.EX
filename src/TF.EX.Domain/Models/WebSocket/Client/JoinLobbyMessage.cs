using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class JoinLobbyMessage
    {
        [JsonPropertyName("JoinLobby")]
        public JoinLobby JoinLobby { get; set; } = new JoinLobby();
    }

    public class JoinLobby
    {
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
