using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class RankedConfirmMessage
    {
        [DataMember(Name = "RankedConfirm")]
        public RankedConfirm RankedConfirm { get; set; } = new RankedConfirm();
    }

    [DataContract]
    public class RankedConfirm
    {
        [DataMember(Name = "seconds")]
        public int Seconds { get; set; }

        [DataMember(Name = "is_decline_free")]
        public bool IsDeclineFree { get; set; }
    }
}
