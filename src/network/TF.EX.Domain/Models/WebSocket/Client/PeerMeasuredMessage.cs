using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class PeerMeasuredMessage
    {
        [DataMember(Name = "PeerMeasured")]
        public PeerMeasured PeerMeasured { get; set; } = new PeerMeasured();
    }

    [DataContract]
    public class PeerMeasured
    {
        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; } = "";
    }
}
