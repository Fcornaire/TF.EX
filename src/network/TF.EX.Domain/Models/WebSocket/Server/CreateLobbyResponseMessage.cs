using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class CreateLobbyResponseMessage
    {

        [DataMember(Name = "CreateLobbyResponse")]
        public CreateLobbyResponse CreateLobbyResponse { get; set; } = new CreateLobbyResponse();
    }

    [DataContract]
    public class CreateLobbyResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }
    }
}
