using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RankedStatusMessage
    {
        [DataMember(Name = "RankedStatus")]
        public RankedStatus RankedStatus { get; set; } = new RankedStatus();
    }

    [DataContract]
    public class RankedStatus
    {
        [DataMember(Name = "queued")]
        public bool Queued { get; set; }

        [DataMember(Name = "searching_standard")]
        public int SearchingStandard { get; set; }

        [DataMember(Name = "searching_wide")]
        public int SearchingWide { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}