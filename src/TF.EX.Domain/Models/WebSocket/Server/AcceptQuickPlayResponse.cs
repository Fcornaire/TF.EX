using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class AcceptQuickPlayResponseMessage
    {
        [JsonPropertyName("AcceptQuickPlayResponse")]
        public AcceptQuickPlayResponse AcceptQuickPlayResponse { get; set; } = new AcceptQuickPlayResponse();
    }

    public class AcceptQuickPlayResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("opponent_name")]
        public string OpponentName { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }
    }
}
