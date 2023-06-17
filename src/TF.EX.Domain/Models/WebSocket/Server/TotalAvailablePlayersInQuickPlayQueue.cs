using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class TotalAvailablePlayersInQuickPlayQueueMessage
    {
        [JsonProperty("TotalAvailablePlayersInQuickPlayQueue")]
        public TotalAvailablePlayersInQuickPlayQueue TotalAvailablePlayersInQuickPlayQueue { get; set; } = new TotalAvailablePlayersInQuickPlayQueue();
    }

    public class TotalAvailablePlayersInQuickPlayQueue
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
