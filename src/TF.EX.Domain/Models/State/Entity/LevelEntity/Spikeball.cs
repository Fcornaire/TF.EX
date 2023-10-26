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
    }
}
