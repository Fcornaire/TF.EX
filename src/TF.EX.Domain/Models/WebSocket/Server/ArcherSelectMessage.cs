using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class ArcherSelectMessage
    {
        [DataMember(Name = "ArcherSelect")]
        public ArcherSelect ArcherSelect { get; set; } = new ArcherSelect();
    }

    [DataContract]
    public class ArcherSelect
    {
    }
}
