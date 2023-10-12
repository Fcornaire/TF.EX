using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class CreateLobbyResponseMessage
    {

        [JsonPropertyName("CreateLobbyResponse")]
        public CreateLobbyResponse CreateLobbyResponse { get; set; } = new CreateLobbyResponse();
    }

    public class CreateLobbyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("lobby")]
        public Lobby Lobby { get; set; }
    }
}
