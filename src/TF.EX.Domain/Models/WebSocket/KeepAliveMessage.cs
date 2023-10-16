using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket
{
    public class KeepAliveMessage
    {
        [JsonPropertyName("KeepAlive")]
        public KeepAlive KeepAlive { get; set; } = new KeepAlive();
    }

    public class KeepAlive
    {
    }
}
