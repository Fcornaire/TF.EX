using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    public class AcceptQuickPlayResponseMessage
    {
        [JsonProperty("AcceptQuickPlayResponse")]
        public AcceptQuickPlayResponse AcceptQuickPlayResponse { get; set; } = new AcceptQuickPlayResponse();
    }

    public class AcceptQuickPlayResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("opponent_name")]
        public string OpponentName { get; set; }

        [JsonProperty("seed")]
        public int Seed { get; set; }
    }
}
