using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class DummyHead
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }

        [Key(2)]
        public Vector2f Speed { get; set; }

        [Key(3)]
        public int RotateSign { get; set; }

        [Key(4)]
        public float ImageRotation { get; set; }

        [Key(5)]
        public float ImageScaleX { get; set; }
    }
}
