using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Server
{
    [DataContract]
    public class SkinChunkServerMessage
    {
        [DataMember(Name = "SkinChunk")]
        public SkinChunkServer SkinChunk { get; set; } = new SkinChunkServer();
    }

    [DataContract]
    public class SkinChunkServer
    {
        [DataMember(Name = "chunk")]
        public Models.WebSocket.SkinChunk Chunk { get; set; } = new Models.WebSocket.SkinChunk();

        [DataMember(Name = "from")]
        public string From { get; set; } = "";
    }
}
