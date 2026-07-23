using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class CrumbleBlock
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
        public bool IsActive { get; set; }

        [Key(5)]
        public float ExplodeCounter { get; set; }
    }
}
