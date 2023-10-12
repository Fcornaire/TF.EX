using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class CreateLobbyMessage
    {
        [JsonPropertyName("CreateLobby")]
        public CreateLobby CreateLobby { get; set; } = new CreateLobby();
    }

    public class CreateLobby
    {
        [JsonPropertyName("lobby")]
        public Lobby Lobby { get; set; }
    }
}
