using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RematchLobbyMessage
    {
        [DataMember(Name = "RematchLobby")]
        public RematchLobby RematchLobby { get; set; } = new RematchLobby();
    }

    [DataContract]
    public class RematchLobby
    {
    }
}
