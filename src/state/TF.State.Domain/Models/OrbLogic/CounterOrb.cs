using MessagePack;

namespace TF.State.Domain.Models.OrbLogic
{
    [MessagePackObject]
    public class CounterOrb
    {
        [Key(0)]
        public float Start { get; set; }
        [Key(1)]
        public float End { get; set; }

        public static CounterOrb Default => new CounterOrb
        {
            Start = -1,
            End = -1
        };
    }
}
