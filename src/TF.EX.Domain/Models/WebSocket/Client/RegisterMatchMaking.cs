using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class RegisterQuickPlayMessage
    {
        [JsonProperty("RegisterQuickPlay")]
        public Register Register { get; set; } = new Register();
    }

    public class Register
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
