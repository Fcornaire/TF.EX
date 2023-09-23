using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class AcceptQuickPlayMessage
    {
        [JsonPropertyName("AcceptQuickPlay")]
        public AcceptQuickPlay AcceptQuickPlay { get; set; } = new AcceptQuickPlay();
    }

    public class AcceptQuickPlay
    {
    }
}
