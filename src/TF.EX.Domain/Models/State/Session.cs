namespace TF.EX.Domain.Models.State
{
    public class Session
    {
        public float RoundEndCounter { get; set; }
        public bool IsEnding { get; set; }
        public Miasma Miasma { get; set; }
        public bool RoundStarted { get; set; }

        public bool IsDone { get; set; }

        public int RoundIndex { get; set; }

        public int[] Scores { get; set; } = new int[4];
        public int[] OldScores { get; set; } = new int[4];
    }
}
