using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Miasma
    {
        [Key(0)]
        public float Counter { get; set; }

        [Key(1)]
        public int Mode { get; set; }

        [Key(2)]
        public float Ticks { get; set; }

        [Key(3)]
        public int Dir { get; set; }

        [Key(4)]
        public bool IsDissipating { get; set; }

        [Key(5)]
        public float DissipateTicks { get; set; }

        [Key(6)]
        public float DissipateStartPercent { get; set; }

        [Key(7)]
        public float DissipateStartSideWeight { get; set; }

        [Key(8)]
        public double ActualDepth { get; set; } = Constants.MIASMA_CUSTOM_DEPTH;

        public static Miasma Default()
        {
            return new Miasma
            {
                Counter = Constants.DEFAULT_MIASMA_COUNTER,
            };
        }
    }
}
