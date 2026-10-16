using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class NoticeMessage
    {
        [DataMember(Name = "Notice")]
        public Notice Notice { get; set; } = new Notice();
    }

    [DataContract]
    public class Notice
    {
        [DataMember(Name = "text")]
        public string Text { get; set; } = "";
    }
}
