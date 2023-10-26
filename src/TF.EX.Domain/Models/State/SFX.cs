using MessagePack;
using Microsoft.Xna.Framework.Audio;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class SFX
    {
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public int Frame { get; set; }
        [Key(2)]
        public float Volume { get; set; }
        [Key(3)]
        public float Pitch { get; set; }
        [Key(4)]
        public float Pan { get; set; }

        [IgnoreMember]
        public SoundEffect Data { get; set; }
    }
}
