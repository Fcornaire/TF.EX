using Newtonsoft.Json;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    public class RegisterDirectMessage
    {

        [JsonProperty("RegisterDirect")]
        public RegisterDirect RegisterDirect { get; set; } = new RegisterDirect();
    }

    public class RegisterDirect
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
