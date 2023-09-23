using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class RegisterDirectResponseMessage
    {
        [JsonPropertyName("RegisterDirectResponse")]
        public RegisterDirectResponse RegisterDirectResponse { get; set; } = new RegisterDirectResponse();
    }

    public class RegisterDirectResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

}
