namespace TF.EX.Domain.Models.State.Orb
{
    public class Dark : IOrb
    {
        public Counter Counter { get; set; }
        public bool IsDarkened { get; set; }
        public float OldDarkAlpha { get; set; }
        public float LightingTartgetAlpha { get; set; }

        public static Dark Default => new Dark
        {
            Counter = Counter.Default,
            IsDarkened = false,
            OldDarkAlpha = 0,
            LightingTartgetAlpha = 0
        };

        public bool IsDefault()
        {
            return Counter.Start == Counter.Default.Start && Counter.End == Counter.Default.End;
        }
    }
}
