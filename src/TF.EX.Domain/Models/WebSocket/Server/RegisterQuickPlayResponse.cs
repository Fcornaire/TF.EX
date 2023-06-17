using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class RegisterQuickPlayResponseMessage
    {
        [JsonProperty("RegisterQuickPlayResponse")]
        public RegisterQuickPlayResponse RegisterQuickPlayResponse { get; set; } = new RegisterQuickPlayResponse();
    }

    public class RegisterQuickPlayResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
