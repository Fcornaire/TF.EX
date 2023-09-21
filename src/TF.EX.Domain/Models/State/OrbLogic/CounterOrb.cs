namespace TF.EX.Domain.Models.State.OrbLogic
{
    public class CounterOrb
    {
        public float Start { get; set; }
        public float End { get; set; }

        public static CounterOrb Default => new CounterOrb
        {
            Start = -1,
            End = -1
        };
    }
}
