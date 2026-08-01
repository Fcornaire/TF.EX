using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class ArcherSelectChoiceMessage
    {
        [DataMember(Name = "ArcherSelectChoice")]
        public ArcherSelectChoice ArcherSelectChoice { get; set; } = new ArcherSelectChoice();
    }

    [DataContract]
    public class ArcherSelectChoice
    {
    }
}
