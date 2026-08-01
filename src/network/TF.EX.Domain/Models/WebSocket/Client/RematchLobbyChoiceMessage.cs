using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class RematchLobbyChoiceMessage
    {
        [DataMember(Name = "RematchLobbyChoice")]
        public RematchLobbyChoice RematchLobby { get; set; } = new RematchLobbyChoice();
    }

    [DataContract]
    public class RematchLobbyChoice
    {
    }
}
