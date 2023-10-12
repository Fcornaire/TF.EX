using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    internal class GetLobbiesResponseMessage
    {
        [JsonPropertyName("GetLobbiesResponse")]
        public GetLobbiesResponse GetLobbiesResponse { get; set; } = new GetLobbiesResponse();
    }

    public class GetLobbiesResponse
    {
        [JsonPropertyName("lobbies")]
        public ICollection<Lobby> Lobbies { get; set; }
    }
}
