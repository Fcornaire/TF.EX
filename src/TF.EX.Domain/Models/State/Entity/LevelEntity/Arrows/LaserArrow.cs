using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class LaserArrow : Arrow
    {
        [Key(22)]
        public int Bounced { get; set; }

        [Key(23)]
        public bool CanBounceIndefinitely { get; set; }
    }
}
