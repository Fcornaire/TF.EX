using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class RegisterDirectMessage
    {

        [JsonPropertyName("RegisterDirect")]
        public RegisterDirect RegisterDirect { get; set; } = new RegisterDirect();
    }

    public class RegisterDirect
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
