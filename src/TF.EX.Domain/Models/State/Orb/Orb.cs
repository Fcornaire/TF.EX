using TF.EX.Domain.Models.State.LevelEntity;

namespace TF.EX.Domain.Models.State.Orb
{
    public interface IOrb
    {
        bool IsDefault();
    }

    public class Orb
    {
        public Time Time { get; set; }
        public Dark Dark { get; set; }
        public LavaControl Lava { get; set; }
        public Counter Space { get; set; }
        public Counter Chaos { get; set; }

        public static Orb Default => new Orb
        {
            Time = Time.Default,
            Dark = Dark.Default,
            Lava = LavaControl.Default,
            Space = Counter.Default,
            Chaos = Counter.Default
        };

        public bool IsDefault() => Time.IsDefault() && Dark.IsDefault() && Lava.IsDefault();

    }
}