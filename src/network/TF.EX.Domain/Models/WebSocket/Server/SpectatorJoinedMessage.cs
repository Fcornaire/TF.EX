using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class SpectatorJoinedMessage
    {
        [DataMember(Name = "SpectatorJoined")]
        public SpectatorJoined SpectatorJoined { get; set; } = new SpectatorJoined();
    }

    [DataContract]
    public class SpectatorJoined
    {
        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; } = "";
    }
}
