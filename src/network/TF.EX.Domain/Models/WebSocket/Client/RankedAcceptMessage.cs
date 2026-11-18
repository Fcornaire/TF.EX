using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class RankedAcceptMessage
    {
        [DataMember(Name = "RankedAccept")]
        public RankedAccept RankedAccept { get; set; } = new RankedAccept();
    }

    [DataContract]
    public class RankedAccept
    {
    }
}
