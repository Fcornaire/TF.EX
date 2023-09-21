namespace TF.EX.Domain.Models.State.OrbLogic
{
    public interface IOrbLogic
    {
        bool IsDefault();
    }

    public class OrbLogic
    {
        public Time Time { get; set; } = Time.Default;
        public Dark Dark { get; set; } = Dark.Default;
        public Space Space { get; set; } = Space.Default;
        public CounterOrb Chaos { get; set; } = CounterOrb.Default;

        public static OrbLogic Default => new OrbLogic
        {
            Time = Time.Default,
            Dark = Dark.Default,
            Space = Space.Default,
            Chaos = CounterOrb.Default
        };

        public bool IsDefault() => Time.IsDefault() && Dark.IsDefault();

    }
}