using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class MoonGlassBlock
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public int Width { get; set; }

        [Key(3)]
        public int Height { get; set; }

        [Key(4)]
        public bool IsCollidable { get; set; }

        [Key(5)]
        public bool IsExploding { get; set; }

        [Key(6)]
        public float ExplodeWaitTicks { get; set; }

        [Key(7)]
        public int EmittedCells { get; set; }
    }
}
