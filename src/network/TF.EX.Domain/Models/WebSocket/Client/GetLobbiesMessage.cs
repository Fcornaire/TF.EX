using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class GetLobbiesMessage
    {
        [DataMember(Name = "GetLobbies")]
        public GetLobbies GetLobbies { get; set; } = new GetLobbies();
    }

    [DataContract]
    public class GetLobbies
    {

    }
}
