using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class MatchWithDirectCodeResponseMessage
    {
        [JsonPropertyName("MatchWithDirectCodeResponse")]
        public MatchWithDirectCodeResponse MatchWithDirectCodeResponse { get; set; } = new MatchWithDirectCodeResponse();
    }

    public class MatchWithDirectCodeResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("opponent_name")]
        public string OpponentName { get; set; }

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }

        [JsonPropertyName("room_chat_id")]
        public string RoomChatId { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }
    }
}
