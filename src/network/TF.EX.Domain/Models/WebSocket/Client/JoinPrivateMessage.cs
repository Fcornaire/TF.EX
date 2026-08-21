using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class JoinPrivateMessage
    {
        [DataMember(Name = "JoinPrivate")]
        public JoinPrivate JoinPrivate { get; set; } = new JoinPrivate();
    }

    [DataContract]
    public class JoinPrivate
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "is_player")]
        public bool IsPlayer { get; set; }
    }
}
