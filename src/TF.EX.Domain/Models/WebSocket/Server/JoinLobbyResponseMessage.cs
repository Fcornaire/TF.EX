using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class JoinLobbyResponseMessage
    {

        [DataMember(Name = "JoinLobbyResponse")]
        public JoinLobbyResponse JoinLobbyResponse { get; set; } = new JoinLobbyResponse();
    }

    [DataContract]
    public class JoinLobbyResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "room_peer_id")]
        public string RoomPeerId { get; set; }

        [DataMember(Name = "room_chat_peer_id")]
        public string RoomChatPeerId { get; set; }
    }
}
