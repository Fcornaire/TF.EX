using Microsoft.Xna.Framework.Audio;
using TF.State.Domain.Models;

namespace TF.State.Domain.Ports
{
    public interface ISFXService
    {
        void AddDesired(SFX toPlay);
        void AddSoundEffect(SoundEffect data, string filename);

        string GetSoundEffectName(SoundEffect data);

        void Clear();
        IEnumerable<SFX> Get();
        void SaveSnapshot(int frame);
        void RestoreSnapshot(int frame);
        void ClearSnapshots();
        void Load(IEnumerable<SFX> sFXes);
        void Reset();
        void Synchronize(int currentFrame, bool isTestMode);
        void UpdateLastRollbackFrame(int frame);
    }
}
