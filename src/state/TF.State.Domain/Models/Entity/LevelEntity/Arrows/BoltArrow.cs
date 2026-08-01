using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class BoltArrow : Arrow
    {
        [Key(23)]
        public Counter CanTurnCounter { get; set; }

        [Key(24)]
        public int Turns { get; set; }
    }
}
