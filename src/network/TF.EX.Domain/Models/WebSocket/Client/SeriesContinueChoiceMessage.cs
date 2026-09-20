using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class SeriesContinueChoiceMessage
    {
        [DataMember(Name = "SeriesContinueChoice")]
        public SeriesContinueChoice SeriesContinueChoice { get; set; } = new SeriesContinueChoice();
    }

    [DataContract]
    public class SeriesContinueChoice
    {
    }
}
