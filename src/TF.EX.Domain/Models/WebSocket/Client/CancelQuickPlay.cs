using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class CancelQuickPlayMessage
    {
        [JsonPropertyName("CancelQuickPlay")]
        public CancelQuickPlay CancelQuickPlay { get; set; } = new CancelQuickPlay();
    }

    public class CancelQuickPlay
    {
    }
}
