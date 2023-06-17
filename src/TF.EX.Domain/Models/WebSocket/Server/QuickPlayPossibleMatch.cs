using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class QuickPlayPossibleMatchMessage
    {
        [JsonProperty("QuickPlayPossibleMatch")]
        public QuickPlayPossibleMatch QuickPlayPossibleMatch { get; set; } = new QuickPlayPossibleMatch();
    }

    public class QuickPlayPossibleMatch
    {
        [JsonProperty("room_id")]
        public string RoomId { get; set; }

        [JsonProperty("room_chat_id")]
        public string RoomChatId { get; set; }

        [JsonProperty("opponent_name")]
        public string OpponentName { get; set; }
    }
}
