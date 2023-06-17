using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class MatchWithDirectCodeResponseMessage
    {
        [JsonProperty("MatchWithDirectCodeResponse")]
        public MatchWithDirectCodeResponse MatchWithDirectCodeResponse { get; set; } = new MatchWithDirectCodeResponse();
    }

    public class MatchWithDirectCodeResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("opponent_name")]
        public string OpponentName { get; set; }

        [JsonProperty("room_id")]
        public string RoomId { get; set; }

        [JsonProperty("room_chat_id")]
        public string RoomChatId { get; set; }

        [JsonProperty("seed")]
        public int Seed { get; set; }
    }
}
