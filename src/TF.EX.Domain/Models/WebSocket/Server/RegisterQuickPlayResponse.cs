using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class RegisterQuickPlayResponseMessage
    {
        [JsonPropertyName("RegisterQuickPlayResponse")]
        public RegisterQuickPlayResponse RegisterQuickPlayResponse { get; set; } = new RegisterQuickPlayResponse();
    }

    public class RegisterQuickPlayResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
