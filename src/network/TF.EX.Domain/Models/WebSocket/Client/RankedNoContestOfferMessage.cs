using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class RankedNoContestOfferMessage
    {
        [DataMember(Name = "RankedNoContestOffer")]
        public RankedNoContestOffer RankedNoContestOffer { get; set; } = new RankedNoContestOffer();
    }

    [DataContract]
    public class RankedNoContestOffer
    {
    }
}
