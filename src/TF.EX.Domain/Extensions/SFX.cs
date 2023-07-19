using Microsoft.Xna.Framework.Audio;
using System.Reflection;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Extensions
{
    public static class SFXExtensions
    {
        public static SoundEffectInstance ToSoundEffectInstance(this SFX self)
        {
            Type SoundEffectInstanceType = typeof(SoundEffectInstance);

            ConstructorInfo soundEffectInstanceConstructor = SoundEffectInstanceType.GetConstructor
                (BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new[] { typeof(SoundEffect) }, null);
            SoundEffectInstance soundEffectInstance = (SoundEffectInstance)soundEffectInstanceConstructor.Invoke(new object[] { self.Data });

            soundEffectInstance.Volume = self.Volume;
            soundEffectInstance.Pitch = self.Pitch;
            soundEffectInstance.Pan = self.Pan;

            return soundEffectInstance;
        }
    }
}
