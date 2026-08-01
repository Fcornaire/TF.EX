using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class GetLobbiesResponseMessage
    {
        [DataMember(Name = "GetLobbiesResponse")]
        public GetLobbiesResponse GetLobbiesResponse { get; set; } = new GetLobbiesResponse();
    }

    [DataContract]
    public class GetLobbiesResponse
    {
        [DataMember(Name = "lobbies")]
        public ICollection<Lobby> Lobbies { get; set; }
    }
}
