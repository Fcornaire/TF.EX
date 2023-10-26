using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Session
    {
        [Key(0)]
        public float RoundEndCounter { get; set; }
        [Key(1)]
        public bool IsEnding { get; set; }
        [Key(2)]
        public Miasma Miasma { get; set; }
        [Key(3)]
        public bool RoundStarted { get; set; }

        [Key(4)]
        public bool IsDone { get; set; }

        [Key(5)]
        public int RoundIndex { get; set; }

        [Key(6)]
        public int[] Scores { get; set; } = new int[4];
        [Key(7)]
        public int[] OldScores { get; set; } = new int[4];
    }
}
