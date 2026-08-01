using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class LobbyUpdateMessage
    {
        [DataMember(Name = "LobbyUpdate")]
        public LobbyUpdate LobbyUpdate { get; set; } = new LobbyUpdate();
    }

    [DataContract]
    public class LobbyUpdate
    {
        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }

    }
}
