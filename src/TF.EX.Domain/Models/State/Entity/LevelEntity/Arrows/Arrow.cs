using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class Arrow
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public Vector2f PositionCounter { get; set; }

        [Key(2)]
        public Vector2f Speed { get; set; }

        [Key(3)]
        public float Direction { get; set; }

        [Key(4)]
        public float ShootingCounter { get; set; }

        [Key(5)]
        public float CannotPickupCounter { get; set; }

        [Key(6)]
        public float CannotCatchCounter { get; set; }

        [Key(7)]
        public Flash Flash { get; set; }

        [Key(8)]
        public ArrowStates State { get; set; }

        [Key(9)]
        public ArrowTypes ArrowType { get; set; }

        [Key(10)]
        public Vector2f StuckDirection { get; set; }

        [Key(11)]
        public int PlayerIndex { get; set; }

        [Key(12)]
        public bool IsCollidable { get; set; }

        [Key(13)]
        public bool IsActive { get; set; }

        [Key(14)]
        public bool IsFrozen { get; set; }

        [Key(15)]
        public bool IsVisible { get; set; }

        [Key(16)]
        public bool HasUnhittableEntity { get; set; }

        [Key(17)]
        public double ActualDepth { get; set; }

        [Key(18)]
        public bool MarkedForRemoval { get; set; }

        [Key(19)]
        public FireControl FireControl { get; set; }


        public static Arrow EmptyArrow()
        {
            var arrow = new Arrow()
            {
                Position = new Vector2f { X = -1, Y = -1 },
                PositionCounter = new Vector2f { X = -1, Y = -1 },
                Speed = new Vector2f { X = -1, Y = -1 },
                Direction = -1,
                ShootingCounter = -1,
                CannotPickupCounter = -1,
                PlayerIndex = -1,
                CannotCatchCounter = -1,
                State = ArrowStates.Shooting,
                ArrowType = ArrowTypes.Normal,
                StuckDirection = new Vector2f { X = -1, Y = -1 },
            };

            return arrow;
        }
    }
}
