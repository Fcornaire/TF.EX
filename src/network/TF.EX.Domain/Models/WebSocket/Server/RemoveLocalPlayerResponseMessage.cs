using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RemoveLocalPlayerResponseMessage
    {
        [DataMember(Name = "RemoveLocalPlayerResponse")]
        public RemoveLocalPlayerResponse RemoveLocalPlayerResponse { get; set; } = new RemoveLocalPlayerResponse();
    }

    [DataContract]
    public class RemoveLocalPlayerResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
