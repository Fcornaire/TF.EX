using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class RankedPingReportMessage
    {
        [DataMember(Name = "RankedPingReport")]
        public RankedPingReport RankedPingReport { get; set; } = new RankedPingReport();
    }

    [DataContract]
    public class RankedPingReport
    {
        [DataMember(Name = "rtt")]
        public int Rtt { get; set; }

        [DataMember(Name = "spike")]
        public int Spike { get; set; }

        [DataMember(Name = "loss_percent")]
        public int LossPercent { get; set; }

        [DataMember(Name = "samples")]
        public int Samples { get; set; }
    }
}
