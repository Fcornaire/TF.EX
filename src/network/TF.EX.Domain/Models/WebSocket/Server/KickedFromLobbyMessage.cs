using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class KickedFromLobbyMessage
    {
        [DataMember(Name = "KickedFromLobby")]
        public KickedFromLobby KickedFromLobby { get; set; } = new KickedFromLobby();
    }

    [DataContract]
    public class KickedFromLobby
    {
        [DataMember(Name = "reason")]
        public string Reason { get; set; } = "";
    }
}
