using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class LeaveLobbyResponseMessage
    {

        [JsonPropertyName("LeaveLobbyResponse")]
        public LeaveLobbyResponse LeaveLobbyResponse { get; set; } = new LeaveLobbyResponse();
    }

    public class LeaveLobbyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
