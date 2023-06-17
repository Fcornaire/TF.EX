
using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class AcceptQuickPlayMessage
    {
        [JsonProperty("AcceptQuickPlay")]
        public AcceptQuickPlay AcceptQuickPlay { get; set; } = new AcceptQuickPlay();
    }

    public class AcceptQuickPlay
    {
    }
}
