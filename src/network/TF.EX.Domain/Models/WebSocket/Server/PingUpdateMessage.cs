using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class PingUpdateMessage
    {
        [DataMember(Name = "PingUpdate")]
        public PingUpdate PingUpdate { get; set; } = new PingUpdate();
    }

    [DataContract]
    public class PingUpdate
    {
        [DataMember(Name = "pings")]
        public ICollection<PlayerPing> Pings { get; set; } = new List<PlayerPing>();
    }
}
