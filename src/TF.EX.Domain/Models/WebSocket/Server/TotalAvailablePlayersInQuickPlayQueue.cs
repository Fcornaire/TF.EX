using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class TotalAvailablePlayersInQuickPlayQueueMessage
    {
        [JsonPropertyName("TotalAvailablePlayersInQuickPlayQueue")]
        public TotalAvailablePlayersInQuickPlayQueue TotalAvailablePlayersInQuickPlayQueue { get; set; } = new TotalAvailablePlayersInQuickPlayQueue();
    }

    public class TotalAvailablePlayersInQuickPlayQueue
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
