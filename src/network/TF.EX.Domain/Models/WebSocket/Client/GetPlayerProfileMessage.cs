using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class GetPlayerProfileMessage
    {
        [DataMember(Name = "GetPlayerProfile")]
        public GetPlayerProfile GetPlayerProfile { get; set; } = new GetPlayerProfile();
    }

    [DataContract]
    public class GetPlayerProfile
    {
        [DataMember(Name = "rank")]
        public int? Rank { get; set; }
    }
}
