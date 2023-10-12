using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    internal class GetLobbiesMessage
    {
        [JsonPropertyName("GetLobbies")]
        public GetLobbies GetLobbies { get; set; } = new GetLobbies();
    }

    public class GetLobbies
    {

    }
}
