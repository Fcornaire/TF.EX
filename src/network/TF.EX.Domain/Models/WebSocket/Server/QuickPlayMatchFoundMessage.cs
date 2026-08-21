using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class QuickPlayMatchFoundMessage
    {
        [DataMember(Name = "QuickPlayMatchFound")]
        public QuickPlayMatchFound QuickPlayMatchFound { get; set; } = new QuickPlayMatchFound();
    }

    [DataContract]
    public class QuickPlayMatchFound
    {
        [DataMember(Name = "lobby")]
        public Lobby Lobby { get; set; }

        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; }
    }
}
