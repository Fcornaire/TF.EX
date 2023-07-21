namespace TF.EX.Domain.Models.State.Orb
{
    public interface IOrb
    {
        bool IsDefault();
    }

    public class Orb
    {
        public Time Time { get; set; } = Time.Default;
        public Dark Dark { get; set; } = Dark.Default;
        public Counter Space { get; set; } = Counter.Default;
        public Counter Chaos { get; set; } = Counter.Default;

        public static Orb Default => new Orb
        {
            Time = Time.Default,
            Dark = Dark.Default,
            Space = Counter.Default,
            Chaos = Counter.Default
        };

        public bool IsDefault() => Time.IsDefault() && Dark.IsDefault();

    }
}