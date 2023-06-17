namespace TF.EX.Domain.Models.State
{
    public class Session
    {
        public float RoundEndCounter { get; set; }
        public bool IsEnding { get; set; }
        public Miasma Miasma { get; set; }
        public bool RoundStarted { get; set; }
    }
}
