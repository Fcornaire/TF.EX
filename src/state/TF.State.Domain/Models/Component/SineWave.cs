using MessagePack;

namespace TF.State.Domain.Models.Component
{
    [MessagePackObject]
    public class SineWave
    {
        [Key(0)]
        public float Counter { get; set; }
    }
}
