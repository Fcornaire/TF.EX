using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Icicle
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }

        [Key(2)]
        public bool Falling { get; set; }

        [Key(3)]
        public bool CanFall { get; set; }

        [Key(4)]
        public float VSpeed { get; set; }

        [Key(5)]
        public int OwnerIndex { get; set; }

        [Key(6)]
        public float CannotHitCounter { get; set; }

        [Key(7)]
        public float FallCounter { get; set; } = -1f;

        [Key(8)]
        public bool HasCannotHit { get; set; }

        [Key(9)]
        public int CannotHitPlayerIndex { get; set; }

        [Key(10)]
        public bool HasCannotHitArrow { get; set; }

        [Key(11)]
        public double CannotHitArrowActualDepth { get; set; }

        [Key(12)]
        public bool IsActive { get; set; }

        [Key(13)]
        public bool IsCollidable { get; set; }

        [Key(14)]
        public bool IsVisible { get; set; }
    }
}
