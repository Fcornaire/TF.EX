using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    public class ArrowBuilder
    {
        private class IntermediateArrow : Arrow
        {
            public bool CanExplode { get; set; }

            public Alarm ExplodeAlarm { get; set; }

            public Sprite<int> NormalSprite { get; set; }

            public Sprite<int> BuriedSprite { get; set; }

            public IntermediateArrow()
            {
                ArrowType = ArrowTypes.Normal;
            }
        }

        private IntermediateArrow arrow;

        public ArrowBuilder()
        {
            arrow = new IntermediateArrow();
        }

        public ArrowBuilder WithActualDepth(double actualDepth)
        {
            arrow.ActualDepth = actualDepth;
            return this;
        }

        public ArrowBuilder WithArrowType(TowerFall.ArrowTypes arrowType)
        {
            arrow.ArrowType = arrowType.ToModel();
            return this;
        }

        public ArrowBuilder WithDirection(float direction)
        {
            arrow.Direction = direction;
            return this;
        }

        public ArrowBuilder WithPlayerIndex(int playerIndex)
        {
            arrow.PlayerIndex = playerIndex;
            return this;
        }

        public ArrowBuilder WithPosition(Vector2f position)
        {
            arrow.Position = position;
            return this;
        }

        public ArrowBuilder WithShootingCounter(float shootingCounter)
        {
            arrow.ShootingCounter = shootingCounter;
            return this;
        }

        public ArrowBuilder WithCannotPickupCounter(float cannotPickupCounter)
        {
            arrow.CannotPickupCounter = cannotPickupCounter;
            return this;
        }

        public ArrowBuilder WithCannotCatchCounter(float cannotCatchCounter)
        {
            arrow.CannotCatchCounter = cannotCatchCounter;
            return this;
        }

        public ArrowBuilder WithSpeed(Vector2f speed)
        {
            arrow.Speed = speed;
            return this;
        }

        public ArrowBuilder WithState(TowerFall.Arrow.ArrowStates state)
        {
            arrow.State = state.ToModel();
            return this;
        }

        public ArrowBuilder WithStuckDirection(Vector2f stuckDirection)
        {
            arrow.StuckDirection = stuckDirection;
            return this;
        }

        public ArrowBuilder WithPositionCounter(Vector2f positionCounter)
        {
            arrow.PositionCounter = positionCounter;
            return this;
        }

        public ArrowBuilder WithIsActive(bool isActive)
        {
            arrow.IsActive = isActive;
            return this;
        }

        public ArrowBuilder WithIsCollidable(bool isCollidable)
        {
            arrow.IsCollidable = isCollidable;
            return this;
        }

        public ArrowBuilder WithIsFrozen(bool isFrozen)
        {
            arrow.IsFrozen = isFrozen;
            return this;
        }

        public ArrowBuilder WithIsVisible(bool isVisible)
        {
            arrow.IsVisible = isVisible;
            return this;
        }

        public ArrowBuilder WithMarkedForRemoval(bool markedForRemoval)
        {
            arrow.MarkedForRemoval = markedForRemoval;
            return this;
        }

        public ArrowBuilder WithFireControl(FireControl fireControl)
        {
            arrow.FireControl = fireControl;
            return this;
        }

        public ArrowBuilder WithFlash(Flash flash)
        {
            arrow.Flash = flash;
            return this;
        }

        public ArrowBuilder WithHasUnhittableEntity(bool hasUnhittableEntity)
        {
            arrow.HasUnhittableEntity = hasUnhittableEntity;
            return this;
        }

        public ArrowBuilder WithCanExplode(bool canExplode)
        {
            arrow.CanExplode = canExplode;
            return this;
        }

        public ArrowBuilder WithExplodeAlarm(Alarm explodeAlarm)
        {
            arrow.ExplodeAlarm = explodeAlarm;
            return this;
        }

        public ArrowBuilder WithNormalSprite(Sprite<int> normalSprite)
        {
            arrow.NormalSprite = normalSprite;
            return this;
        }

        public ArrowBuilder WithBuriedSprite(Sprite<int> buriedSprite)
        {
            arrow.BuriedSprite = buriedSprite;
            return this;
        }

        public ArrowBuilder WithBuriedIn(ArrowCushion buriedIn)
        {
            arrow.BuriedIn = buriedIn;
            return this;
        }

        public Arrow Build()
        {
            switch (arrow.ArrowType)
            {
                case ArrowTypes.Normal:
                    return new DefaultArrow
                    {
                        ActualDepth = arrow.ActualDepth,
                        ArrowType = arrow.ArrowType,
                        Direction = arrow.Direction,
                        PlayerIndex = arrow.PlayerIndex,
                        Position = arrow.Position,
                        ShootingCounter = arrow.ShootingCounter,
                        CannotPickupCounter = arrow.CannotPickupCounter,
                        CannotCatchCounter = arrow.CannotCatchCounter,
                        Speed = arrow.Speed,
                        State = arrow.State,
                        StuckDirection = arrow.StuckDirection,
                        PositionCounter = arrow.PositionCounter,
                        IsActive = arrow.IsActive,
                        IsCollidable = arrow.IsCollidable,
                        IsFrozen = arrow.IsFrozen,
                        IsVisible = arrow.IsVisible,
                        MarkedForRemoval = arrow.MarkedForRemoval,
                        FireControl = arrow.FireControl,
                        Flash = arrow.Flash,
                        HasUnhittableEntity = arrow.HasUnhittableEntity,
                        BuriedIn = arrow.BuriedIn
                    };
                case ArrowTypes.Bomb:
                    return new BombArrow
                    {
                        ActualDepth = arrow.ActualDepth,
                        ArrowType = arrow.ArrowType,
                        Direction = arrow.Direction,
                        PlayerIndex = arrow.PlayerIndex,
                        Position = arrow.Position,
                        ShootingCounter = arrow.ShootingCounter,
                        CannotPickupCounter = arrow.CannotPickupCounter,
                        CannotCatchCounter = arrow.CannotCatchCounter,
                        Speed = arrow.Speed,
                        State = arrow.State,
                        StuckDirection = arrow.StuckDirection,
                        PositionCounter = arrow.PositionCounter,
                        IsActive = arrow.IsActive,
                        IsCollidable = arrow.IsCollidable,
                        IsFrozen = arrow.IsFrozen,
                        IsVisible = arrow.IsVisible,
                        MarkedForRemoval = arrow.MarkedForRemoval,
                        FireControl = arrow.FireControl,
                        Flash = arrow.Flash,
                        HasUnhittableEntity = arrow.HasUnhittableEntity,
                        BuriedIn = arrow.BuriedIn,
                        BuriedSprite = arrow.BuriedSprite,
                        CanExplode = arrow.CanExplode,
                        ExplodeAlarm = arrow.ExplodeAlarm,
                        NormalSprite = arrow.NormalSprite
                    };
                default:
                    return arrow;
            }
        }
    }
}
