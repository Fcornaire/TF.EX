using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
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
    }
}
