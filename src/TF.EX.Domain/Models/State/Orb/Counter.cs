namespace TF.EX.Domain.Models.State.Orb
{
    public class Counter
    {
        public float Start { get; set; }
        public float End { get; set; }

        public static Counter Default => new Counter
        {
            Start = -1,
            End = -1
        };
    }
}
