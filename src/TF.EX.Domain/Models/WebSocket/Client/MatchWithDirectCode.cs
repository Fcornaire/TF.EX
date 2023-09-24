using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class MatchWithDirectCodeMessage
    {
        [JsonPropertyName("MatchWithDirectCode")]
        public MatchWithDirectCode MatchWithDirectCode { get; set; } = new MatchWithDirectCode();
    }

    public class MatchWithDirectCode
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }
}
