using MessagePack;

namespace TF.EX.Domain.Models.State.Component
{
    [MessagePackObject]
    public class SineWave
    {
        [Key(0)]
        public float Counter { get; set; }
    }
}
