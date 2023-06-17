using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class RegisterDirectResponseMessage
    {
        [JsonProperty("RegisterDirectResponse")]
        public RegisterDirectResponse RegisterDirectResponse { get; set; } = new RegisterDirectResponse();
    }

    public class RegisterDirectResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

}
