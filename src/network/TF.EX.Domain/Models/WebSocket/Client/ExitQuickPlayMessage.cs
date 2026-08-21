using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class ExitQuickPlayMessage
    {
        [DataMember(Name = "ExitQuickPlay")]
        public ExitQuickPlay ExitQuickPlay { get; set; } = new ExitQuickPlay();
    }

    [DataContract]
    public class ExitQuickPlay
    {
    }
}
