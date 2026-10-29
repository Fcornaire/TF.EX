using TF.EX.Domain.Models.Skin;
using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface ISkinStreamService
    {
        ArtifactSkinBundle GetOrBuildArcherBundle(int archerIndex, int archerAltIndex);

        IReadOnlyList<ArtifactSkinBundle> GetPublished();
        void MarkPublished(int localIndex, ArtifactSkinBundle bundle);

        void ReceiveChunk(string fromPeerId, SkinChunk chunk);

        ArcherSkinBundle GetBundle(string customArcherId);

        byte[] InflateRgba(SkinImage image);

        void Reset();
    }
}
