using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class CreateLobbyMessage
    {
        [DataMember(Name = "CreateLobby")]
        public CreateLobby CreateLobby { get; set; } = new CreateLobby();
    }

    [DataContract]
    public class CreateLobby
    {
        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }

        [DataMember(Name = "local_players")]
        public int LocalPlayers { get; set; } = 1;
    }
}
