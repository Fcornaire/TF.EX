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
        public Counter Space { get; set; } = Counter.Default;
        public Counter Chaos { get; set; } = Counter.Default;

        public static OrbLogic Default => new OrbLogic
        {
            Time = Time.Default,
            Dark = Dark.Default,
            Space = Counter.Default,
            Chaos = Counter.Default
        };

        public bool IsDefault() => Time.IsDefault() && Dark.IsDefault();

    }
}