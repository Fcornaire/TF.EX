using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Orb
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public Vector2f PositionCounter { get; set; }
        [Key(2)]
        public float VSpeed { get; set; }
        [Key(3)]
        public float SineCounter { get; set; }
        [Key(4)]
        public double ActualDepth { get; set; }
        [Key(5)]
        public bool IsCollidable { get; set; }
        [Key(6)]
        public bool IsFalling { get; set; }

        [Key(7)]
        public int OwnerIndex { get; set; }
    }
}
