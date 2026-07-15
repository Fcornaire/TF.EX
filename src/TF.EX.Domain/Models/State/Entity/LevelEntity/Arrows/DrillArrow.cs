using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class DrillArrow : Arrow
    {
        [Key(23)]
        public bool HasDrilled { get; set; }

        [Key(24)]
        public bool NaivePush { get; set; }
    }
}
