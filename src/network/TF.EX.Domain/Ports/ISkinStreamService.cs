using TF.EX.Domain.Models.Skin;
using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface ISkinStreamService
    {
        ArtifactSkinBundle GetOrBuildArcherBundle(int archerIndex, int archerAltIndex);

        ArtifactSkinBundle GetLastPublished();
        void MarkPublished(ArtifactSkinBundle bundle);

        void ReceiveChunk(string fromPeerId, SkinChunk chunk);

        ArcherSkinBundle GetBundle(string customArcherId);

        byte[] InflateRgba(SkinImage image);

        void Reset();
    }
}
