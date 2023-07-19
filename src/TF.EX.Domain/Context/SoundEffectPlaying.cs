using Microsoft.Xna.Framework.Audio;

namespace TF.EX.Domain.Context
{

    public class SoundEffectPlaying
    {
        public string Name { get; set; }
        public int Frame { get; set; }
        public SoundEffectInstance SoundEffectInstance { get; set; }
    }
}
