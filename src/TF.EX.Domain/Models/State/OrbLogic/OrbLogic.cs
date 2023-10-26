using MessagePack;

namespace TF.EX.Domain.Models.State.OrbLogic
{
    public interface IOrbLogic
    {
        bool IsDefault();
    }

    [MessagePackObject]
    public class OrbLogic
    {
        [Key(0)]
        public Time Time { get; set; } = Time.Default;
        [Key(1)]
        public Dark Dark { get; set; } = Dark.Default;
        [Key(2)]
        public Space Space { get; set; } = Space.Default;
        [Key(3)]
        public CounterOrb Chaos { get; set; } = CounterOrb.Default;
    }
}