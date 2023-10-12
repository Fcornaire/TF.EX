using System.Text.Json.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    internal class LeaveLobbyMessage
    {
        [JsonPropertyName("LeaveLobby")]
        public LeaveLobby LeaveLobby { get; set; } = new LeaveLobby();
    }

    public class LeaveLobby
    {
    }
}
