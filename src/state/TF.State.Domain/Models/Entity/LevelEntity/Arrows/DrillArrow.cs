using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
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
