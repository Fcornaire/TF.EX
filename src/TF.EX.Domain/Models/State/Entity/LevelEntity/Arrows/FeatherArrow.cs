using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class FeatherArrow : Arrow
    {
        [Key(23)]
        public SineWave MoveSine { get; set; }

        [Key(24)]
        public Vector2f Perpendicular { get; set; }
    }
}
