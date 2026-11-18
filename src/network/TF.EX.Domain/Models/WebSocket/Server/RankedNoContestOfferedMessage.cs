using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RankedNoContestOfferedMessage
    {
        [DataMember(Name = "RankedNoContestOffered")]
        public RankedNoContestOffered RankedNoContestOffered { get; set; } = new RankedNoContestOffered();
    }

    [DataContract]
    public class RankedNoContestOffered
    {
        [DataMember(Name = "seconds")]
        public int Seconds { get; set; }
    }
}
