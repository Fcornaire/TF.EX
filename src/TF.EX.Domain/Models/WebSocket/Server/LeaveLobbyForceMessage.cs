using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class LeaveLobbyForceMessage
    {
        [DataMember(Name = "LeaveLobby")]
        public LeaveLobbyForce LeaveLobbyForce { get; set; } = new LeaveLobbyForce();
    }

    [DataContract]
    public class LeaveLobbyForce
    {
    }
}
