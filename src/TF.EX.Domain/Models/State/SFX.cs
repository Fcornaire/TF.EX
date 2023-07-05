using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;

namespace TF.EX.Domain.Models.State
{
    public class SFX
    {
        public string Name { get; set; }
        public int Frame { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public float Pan { get; set; }

        [JsonIgnore]
        public SoundEffect Data { get; set; }
    }
}
