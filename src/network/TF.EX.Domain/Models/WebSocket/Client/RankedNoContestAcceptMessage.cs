using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class RankedNoContestAcceptMessage
    {
        [DataMember(Name = "RankedNoContestAccept")]
        public RankedNoContestAccept RankedNoContestAccept { get; set; } = new RankedNoContestAccept();
    }

    [DataContract]
    public class RankedNoContestAccept
    {
    }
}
