using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
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
