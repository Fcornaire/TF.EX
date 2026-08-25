using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class TechnoMissile
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f Normal { get; set; }

        [Key(3)]
        public float Speed { get; set; }

        [Key(4)]
        public int TargetIndex { get; set; }

        [Key(5)]
        public float ExplodeCounter { get; set; }

        [Key(6)]
        public float CollidableCounter { get; set; }
    }
}
