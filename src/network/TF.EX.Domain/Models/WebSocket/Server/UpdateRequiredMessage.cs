using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class UpdateRequiredMessage
    {
        [DataMember(Name = "UpdateRequired")]
        public UpdateRequired UpdateRequired { get; set; } = new UpdateRequired();
    }

    [DataContract]
    public class UpdateRequired
    {
        [DataMember(Name = "version")]
        public string Version { get; set; } = "";
    }
}
