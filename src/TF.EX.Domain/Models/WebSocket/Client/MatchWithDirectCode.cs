using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class MatchWithDirectCodeMessage
    {
        [JsonProperty("MatchWithDirectCode")]
        public MatchWithDirectCode MatchWithDirectCode { get; set; } = new MatchWithDirectCode();
    }

    public class MatchWithDirectCode
    {
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
