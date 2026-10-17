using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    /// <summary>
    /// Another player on this device: the server seats a new Player with this connection's RoomPeerId
    /// </summary>
    [DataContract]
    public class AddLocalPlayerMessage
    {
        [DataMember(Name = "AddLocalPlayer")]
        public AddLocalPlayer AddLocalPlayer { get; set; } = new AddLocalPlayer();
    }

    [DataContract]
    public class AddLocalPlayer
    {
        [DataMember(Name = "player")]
        public Player Player { get; set; }
    }

    [DataContract]
    public class RemoveLocalPlayerMessage
    {
        [DataMember(Name = "RemoveLocalPlayer")]
        public RemoveLocalPlayer RemoveLocalPlayer { get; set; } = new RemoveLocalPlayer();
    }

    [DataContract]
    public class RemoveLocalPlayer
    {
        [DataMember(Name = "seat")]
        public int Seat { get; set; }
    }
}
