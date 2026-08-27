using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class SkinChunkMessage
    {
        [DataMember(Name = "SkinChunk")]
        public SkinChunkSend SkinChunk { get; set; } = new SkinChunkSend();
    }

    [DataContract]
    public class SkinChunkSend
    {
        [DataMember(Name = "chunk")]
        public SkinChunk Chunk { get; set; } = new SkinChunk();
    }
}
