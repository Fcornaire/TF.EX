using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    public class Arrow
    {
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public Vector2f Speed { get; set; }
        public float Direction { get; set; }
        public float ShootingCounter { get; set; }
        public float CannotPickupCounter { get; set; }
        public float CannotCatchCounter { get; set; }
        public Flash Flash { get; set; }
        public ArrowStates State { get; set; }
        public ArrowTypes ArrowType { get; set; }
        public Vector2f StuckDirection { get; set; }
        public int PlayerIndex { get; set; }
        public bool IsCollidable { get; set; }
        public bool IsActive { get; set; }
        public bool IsFrozen { get; set; }
        public bool IsVisible { get; set; }
        public bool HasUnhittableEntity { get; set; }
        public double ActualDepth { get; set; }
        public bool MarkedForRemoval { get; set; }
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
