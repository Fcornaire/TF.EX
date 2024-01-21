using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class CrackedWall
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public Vector2f PositionCounter { get; set; }
        [Key(2)]
        public double ActualDepth { get; set; }
        [Key(3)]
        public float ExplodeCounter { get; set; }
        [Key(4)]
        public Vector2f ExplodeNormal { get; set; }
        [Key(5)]
        public bool IsActive { get; set; }
        [Key(6)]
        public bool IsCollidable { get; set; }
    }
}
