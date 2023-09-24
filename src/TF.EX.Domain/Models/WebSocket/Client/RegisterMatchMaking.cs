using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class RegisterQuickPlayMessage
    {
        [JsonPropertyName("RegisterQuickPlay")]
        public Register Register { get; set; } = new Register();
    }

    public class Register
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
