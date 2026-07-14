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

            public int Bounced { get; set; }

            public bool CanBounceIndefinitely { get; set; }

            public bool CanDie { get; set; }
            public bool IsUsed { get; set; }
            public BrambleSpreadState BrambleSpread { get; set; }


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

        public ArrowBuilder WithTravelFrames(float travelFrames)
        {
            arrow.TravelFrames = travelFrames;
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

        public ArrowBuilder WithStuckToActualDepth(double stuckToActualDepth)
        {
            arrow.StuckToActualDepth = stuckToActualDepth;
            return this;
        }


        public void WithBounced(int bounced)
        {
            arrow.Bounced = bounced;
        }

        public void WithCanBounceIndefinitely(bool canBounceIndefinitely)
        {
            arrow.CanBounceIndefinitely = canBounceIndefinitely;
        }

        public void WithCanDie(bool canDie)
        {
            arrow.CanDie = canDie;
        }

        public void WithIsUsed(bool isUsed)
        {
            arrow.IsUsed = isUsed;
        }

        public void WithBrambleSpread(BrambleSpreadState brambleSpread)
        {
            arrow.BrambleSpread = brambleSpread;
        }

        public Arrow Build()
        {
            Arrow built;

            switch (arrow.ArrowType)
            {
                case ArrowTypes.Normal:
                    built = new DefaultArrow();
                    break;
                case ArrowTypes.Bomb:
                    built = new BombArrow();
                    break;
                case ArrowTypes.Laser:
                    built = new LaserArrow();
                    break;
                case ArrowTypes.Bramble:
                    built = new BrambleArrow();
                    break;
                default:
                    throw new Exception("Unknown arrow type");
            }

            built.ActualDepth = arrow.ActualDepth;
            built.ArrowType = arrow.ArrowType;
            built.Direction = arrow.Direction;
            built.PlayerIndex = arrow.PlayerIndex;
            built.Position = arrow.Position;
            built.ShootingCounter = arrow.ShootingCounter;
            built.CannotPickupCounter = arrow.CannotPickupCounter;
            built.CannotCatchCounter = arrow.CannotCatchCounter;
            built.Speed = arrow.Speed;
            built.State = arrow.State;
            built.StuckDirection = arrow.StuckDirection;
            built.PositionCounter = arrow.PositionCounter;
            built.IsActive = arrow.IsActive;
            built.IsCollidable = arrow.IsCollidable;
            built.IsFrozen = arrow.IsFrozen;
            built.IsVisible = arrow.IsVisible;
            built.MarkedForRemoval = arrow.MarkedForRemoval;
            built.FireControl = arrow.FireControl;
            built.Flash = arrow.Flash;
            built.HasUnhittableEntity = arrow.HasUnhittableEntity;
            built.BuriedIn = arrow.BuriedIn;
            built.StuckToActualDepth = arrow.StuckToActualDepth;
            built.TravelFrames = arrow.TravelFrames;

            if (built is BombArrow bombArrow)
            {
                bombArrow.BuriedSprite = arrow.BuriedSprite;
                bombArrow.CanExplode = arrow.CanExplode;
                bombArrow.ExplodeAlarm = arrow.ExplodeAlarm;
                bombArrow.NormalSprite = arrow.NormalSprite;
            }

            if (built is LaserArrow laserArrow)
            {
                laserArrow.Bounced = arrow.Bounced;
                laserArrow.CanBounceIndefinitely = arrow.CanBounceIndefinitely;
            }

            if (built is BrambleArrow brambleArrow)
            {
                brambleArrow.CanDie = arrow.CanDie;
                brambleArrow.IsUsed = arrow.IsUsed;
                brambleArrow.BrambleSpread = arrow.BrambleSpread;
            }

            return built;
        }
    }
}
