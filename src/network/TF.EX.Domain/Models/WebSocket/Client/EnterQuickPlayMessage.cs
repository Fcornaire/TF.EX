using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class EnterQuickPlayMessage
    {
        [DataMember(Name = "EnterQuickPlay")]
        public EnterQuickPlay EnterQuickPlay { get; set; } = new EnterQuickPlay();
    }

    [DataContract]
    public class EnterQuickPlay
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "is_wide")]
        public bool IsWide { get; set; }
    }
}
