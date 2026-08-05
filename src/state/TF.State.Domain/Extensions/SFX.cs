using Microsoft.Xna.Framework.Audio;
using System.Reflection;
using TF.State.Domain.Models;

namespace TF.State.Domain.Extensions
{
    public static class SFXExtensions
    {
        public static SoundEffectInstance ToSoundEffectInstance(this SFX self)
        {
            Type SoundEffectInstanceType = typeof(SoundEffectInstance);

            ConstructorInfo soundEffectInstanceConstructor = SoundEffectInstanceType.GetConstructor
                (BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new[] { typeof(SoundEffect) }, null);
            SoundEffectInstance soundEffectInstance = (SoundEffectInstance)soundEffectInstanceConstructor.Invoke(new object[] { self.Data });

            soundEffectInstance.Volume = Math.Clamp(self.Volume * Monocle.Audio.MasterVolume, 0f, 1f);
            soundEffectInstance.Pitch = Math.Clamp(self.Pitch + (self.ObeysMasterPitch ? Monocle.Audio.MasterPitch : 0f), -1f, 1f);
            soundEffectInstance.Pan = self.Pan;

            return soundEffectInstance;
        }
    }
}
