using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
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
