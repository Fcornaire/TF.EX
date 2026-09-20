using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class SeriesPickMapMessage
    {
        [DataMember(Name = "SeriesPickMap")]
        public SeriesPickMap SeriesPickMap { get; set; } = new SeriesPickMap();
    }

    [DataContract]
    public class SeriesPickMap
    {
        [DataMember(Name = "map_id")]
        public int MapId { get; set; }
    }
}
