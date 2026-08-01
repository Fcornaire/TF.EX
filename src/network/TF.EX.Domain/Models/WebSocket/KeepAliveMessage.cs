using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket
{
    [DataContract]
    public class KeepAliveMessage
    {
        [DataMember(Name = "KeepAlive")]
        public KeepAlive KeepAlive { get; set; } = new KeepAlive();
    }

    [DataContract]
    public class KeepAlive
    {
    }
}
