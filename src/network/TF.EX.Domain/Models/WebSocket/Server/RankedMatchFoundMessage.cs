using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RankedMatchFoundMessage
    {
        [DataMember(Name = "RankedMatchFound")]
        public RankedMatchFound RankedMatchFound { get; set; } = new RankedMatchFound();
    }

    [DataContract]
    public class RankedMatchFound
    {
        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }

        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; }
    }
}