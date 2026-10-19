using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class ExitRankedMessage
    {
        [DataMember(Name = "ExitRanked")]
        public ExitRanked ExitRanked { get; set; } = new ExitRanked();
    }

    [DataContract]
    public class ExitRanked
    {
    }
}