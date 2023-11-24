using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class DefaultArrow : Arrow
    {
        public static DefaultArrow EmptyArrow()
        {
            var arrow = new DefaultArrow()
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
