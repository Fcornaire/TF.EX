namespace TF.EX.Domain.Models.Skin
{
    //Skin to send
    public class ArtifactSkinBundle
    {
        public string BundleId { get; set; } = "";
        public string CustomArcherId { get; set; } = "";
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
    }
}
