using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class SnowClump
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }

        [Key(2)]
        public bool Melting { get; set; }

        [Key(3)]
        public float Alpha { get; set; }

        [Key(4)]
        public double SolidActualDepth { get; set; } = -1;

        [Key(5)]
        public Vector2f SolidOffset { get; set; }

        [Key(6)]
        public bool IsActive { get; set; }

        [Key(7)]
        public bool IsCollidable { get; set; }

        [Key(8)]
        public bool IsVisible { get; set; }
    }
}
