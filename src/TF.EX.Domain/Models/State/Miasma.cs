using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Miasma
    {
        [Key(0)]
        public float Counter { get; set; }
        [Key(1)]
        public int CoroutineTimer { get; set; }

        [Key(2)]
        public bool IsDissipating { get; set; }
        [Key(3)]
        public int DissipateTimer { get; set; }

        [Key(4)]
        public float Percent { get; set; }

        [Key(5)]
        public float SideWeight { get; set; }

        [Key(6)]
        public double ActualDepth { get; set; } = Constants.MIASMA_CUSTOM_DEPTH;

        [Key(7)]
        public Dictionary<int, float> Frame_TimeMult { get; set; } = new Dictionary<int, float>();

        public static Miasma Default()
        {
            return new Miasma
            {
                Counter = Constants.DEFAULT_MIASMA_COUNTER,
                CoroutineTimer = Constants.DEFAULT_COUROUTINE_TIMER,
            };
        }
    }
}
