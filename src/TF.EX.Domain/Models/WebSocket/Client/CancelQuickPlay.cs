using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class CancelQuickPlayMessage
    {
        [JsonProperty("CancelQuickPlay")]
        public CancelQuickPlay CancelQuickPlay { get; set; } = new CancelQuickPlay();
    }

    public class CancelQuickPlay
    {
    }
}
