using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class IdentifyMessage
    {
        [DataMember(Name = "Identify")]
        public Identify Identify { get; set; } = new Identify();
    }

    [DataContract]
    public class Identify
    {
        [DataMember(Name = "player_id")]
        public string PlayerId { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }
    }
}
