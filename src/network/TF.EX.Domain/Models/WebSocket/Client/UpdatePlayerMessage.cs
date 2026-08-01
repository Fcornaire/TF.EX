using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class UpdatePlayerMessage
    {
        [DataMember(Name = "UpdatePlayer")]
        public UpdatePlayer UpdatePlayer { get; set; } = new UpdatePlayer();
    }

    [DataContract]
    public class UpdatePlayer
    {
        [DataMember(Name = "player")]
        public Player Player { get; set; }
    }
}
