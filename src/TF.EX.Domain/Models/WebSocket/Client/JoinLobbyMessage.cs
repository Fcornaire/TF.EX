using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class JoinLobbyMessage
    {
        [DataMember(Name = "JoinLobby")]
        public JoinLobby JoinLobby { get; set; } = new JoinLobby();
    }

    [DataContract]
    public class JoinLobby
    {
        [DataMember(Name = "room_id")]
        public string RoomId { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "is_player")]
        public bool IsPlayer { get; set; }
    }
}
