using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
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
