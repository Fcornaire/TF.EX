using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class PrivateJoinResultMessage
    {
        [DataMember(Name = "PrivateJoinResult")]
        public PrivateJoinResult PrivateJoinResult { get; set; } = new PrivateJoinResult();
    }

    [DataContract]
    public class PrivateJoinResult
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; }

        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }
    }
}
