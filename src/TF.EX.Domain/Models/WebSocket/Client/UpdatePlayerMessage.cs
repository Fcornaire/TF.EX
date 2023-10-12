using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    internal class UpdatePlayerMessage
    {
        [JsonPropertyName("UpdatePlayer")]
        public UpdatePlayer UpdatePlayer { get; set; } = new UpdatePlayer();
    }

    public class UpdatePlayer
    {
        [JsonPropertyName("player")]
        public Player Player { get; set; }
    }
}
