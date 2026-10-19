using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class EnterRankedMessage
    {
        [DataMember(Name = "EnterRanked")]
        public EnterRanked EnterRanked { get; set; } = new EnterRanked();
    }

    [DataContract]
    public class EnterRanked
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "is_wide")]
        public bool IsWide { get; set; }
    }
}