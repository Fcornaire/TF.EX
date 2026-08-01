using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class LaserArrow : Arrow
    {
        [Key(23)]
        public int Bounced { get; set; }

        [Key(24)]
        public bool CanBounceIndefinitely { get; set; }
    }
}
