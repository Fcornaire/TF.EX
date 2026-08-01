using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class LeaveLobbyResponseMessage
    {

        [DataMember(Name = "LeaveLobbyResponse")]
        public LeaveLobbyResponse LeaveLobbyResponse { get; set; } = new LeaveLobbyResponse();
    }

    [DataContract]
    public class LeaveLobbyResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
