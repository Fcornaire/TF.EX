using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class AddLocalPlayerResponseMessage
    {
        [DataMember(Name = "AddLocalPlayerResponse")]
        public AddLocalPlayerResponse AddLocalPlayerResponse { get; set; } = new AddLocalPlayerResponse();
    }

    [DataContract]
    public class AddLocalPlayerResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
