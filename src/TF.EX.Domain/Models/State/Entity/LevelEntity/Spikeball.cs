using TF.EX.Domain.Models.State.Monocle;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Spikeball
    {
        public float RotatePercent { get; set; }
        public bool IsFirstHalf { get; set; }
        public Counter ShakeCounter { get; set; }
    }
}
