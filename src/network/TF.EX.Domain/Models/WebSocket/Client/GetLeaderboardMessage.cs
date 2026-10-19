using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class GetLeaderboardMessage
    {
        [DataMember(Name = "GetLeaderboard")]
        public GetLeaderboard GetLeaderboard { get; set; } = new GetLeaderboard();
    }

    [DataContract]
    public class GetLeaderboard
    {

    }
}
