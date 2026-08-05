using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Spikeball
    {
        [Key(0)]
        public float RotatePercent { get; set; }
        [Key(1)]
        public bool IsFirstHalf { get; set; }
        [Key(2)]
        public Counter ShakeCounter { get; set; }
        [Key(3)]
        public float SpinTimer { get; set; }
        [Key(4)]
        public double ActualDepth { get; set; }
        [Key(5)]
        public float SpinRate { get; set; }
    }
}
