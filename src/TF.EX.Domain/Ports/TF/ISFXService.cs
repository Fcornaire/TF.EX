using Microsoft.Xna.Framework.Audio;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface ISFXService
    {
        void AddDesired(SFX toPlay);
        void AddSoundEffect(SoundEffect data, string filename);
        void Clear();
        IEnumerable<SFX> Get();
        void Load(IEnumerable<SFX> sFXes);
        void Synchronize(int currentFrame, bool isTestMode);
        void UpdateLastRollbackFrame(int frame);
    }
}
