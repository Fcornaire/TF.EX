using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    internal class LobbyUpdateMessage
    {
        [JsonPropertyName("LobbyUpdate")]
        public LobbyUpdate LobbyUpdate { get; set; } = new LobbyUpdate();
    }

    public class LobbyUpdate
    {
        [JsonPropertyName("lobby")]
        public Lobby Lobby { get; set; }

    }
}
