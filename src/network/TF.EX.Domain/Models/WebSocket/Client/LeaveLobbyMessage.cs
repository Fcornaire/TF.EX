using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class LeaveLobbyMessage
    {
        [DataMember(Name = "LeaveLobby")]
        public LeaveLobby LeaveLobby { get; set; } = new LeaveLobby();
    }

    [DataContract]
    public class LeaveLobby
    {
    }
}
