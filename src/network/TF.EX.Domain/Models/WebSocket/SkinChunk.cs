using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket
{
    [DataContract]
    public class SkinChunk
    {
        [DataMember(Name = "bundle_id")]
        public string BundleId { get; set; } = "";

        [DataMember(Name = "custom_archer_id")]
        public string CustomArcherId { get; set; } = "";

        [DataMember(Name = "chunk_index")]
        public uint ChunkIndex { get; set; }

        [DataMember(Name = "chunk_count")]
        public uint ChunkCount { get; set; }

        [DataMember(Name = "data")]
        public string Data { get; set; } = "";
    }
}
