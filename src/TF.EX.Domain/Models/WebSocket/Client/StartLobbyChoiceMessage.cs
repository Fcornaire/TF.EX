using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class StartLobbyChoiceMessage
    {
        [DataMember(Name = "StartLobbyChoice")]
        public StartLobbyChoice StartLobbyChoice { get; set; } = new StartLobbyChoice();
    }

    [DataContract]
    public class StartLobbyChoice
    {
    }
}
