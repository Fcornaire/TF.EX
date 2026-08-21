using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class QuickPlayStatusMessage
    {
        [DataMember(Name = "QuickPlayStatus")]
        public QuickPlayStatus QuickPlayStatus { get; set; } = new QuickPlayStatus();
    }

    [DataContract]
    public class QuickPlayStatus
    {
        [DataMember(Name = "queued")]
        public bool Queued { get; set; }

        [DataMember(Name = "searching")]
        public int Searching { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}
