using MessagePack;

namespace TF.EX.Domain.Models.State.OrbLogic
{
    [MessagePackObject]
    public class Dark : IOrbLogic
    {
        [Key(0)]
        public CounterOrb Counter { get; set; }
        [Key(1)]
        public bool IsDarkened { get; set; }
        [Key(2)]
        public float OldDarkAlpha { get; set; }
        [Key(3)]
        public float LightingTartgetAlpha { get; set; }

        public static Dark Default => new Dark
        {
            Counter = CounterOrb.Default,
            IsDarkened = false,
            OldDarkAlpha = 0,
            LightingTartgetAlpha = 0
        };

        public bool IsDefault()
        {
            return Counter.Start == CounterOrb.Default.Start && Counter.End == CounterOrb.Default.End;
        }
    }
}
