using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class QuickPlayPossibleMatchMessage
    {
        [JsonPropertyName("QuickPlayPossibleMatch")]
        public QuickPlayPossibleMatch QuickPlayPossibleMatch { get; set; } = new QuickPlayPossibleMatch();
    }

    public class QuickPlayPossibleMatch
    {
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }

        [JsonPropertyName("room_chat_id")]
        public string RoomChatId { get; set; }

        [JsonPropertyName("opponent_name")]
        public string OpponentName { get; set; }
    }
}
