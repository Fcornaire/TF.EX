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
    }
}