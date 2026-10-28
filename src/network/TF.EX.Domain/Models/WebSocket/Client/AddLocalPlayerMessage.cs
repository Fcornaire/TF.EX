using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
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
}
