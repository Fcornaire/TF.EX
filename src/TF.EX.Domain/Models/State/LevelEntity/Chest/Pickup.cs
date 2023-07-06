namespace TF.EX.Domain.Models.State.LevelEntity.Chest
{
    public class Pickup
    {
        public bool IsCollidable { get; set; }
        public PickupState Type { get; set; }
        public Vector2f Position { get; set; }
        public Vector2f TargetPosition { get; set; }
        public float SineCounter { get; set; }
        public float TargetPositionTimer { get; set; }
        public float CollidableTimer { get; set; }
        public int PlayerIndex { get; set; }
        public double ActualDepth { get; set; }
        public bool MarkedForRemoval { get; set; }

        public Sprite<int> ShieldSprite { get; set; }

        public static Pickup Empty()
        {
            return new Pickup
            {
                Position = new Vector2f { x = -1, y = -1 },
                TargetPosition = new Vector2f { x = -1, y = -1 },
                PlayerIndex = -1,
                Type = PickupState.Arrows,
                TargetPositionTimer = -1,
                CollidableTimer = -1,
                IsCollidable = false,
                SineCounter = -1,
            };
        }
    }
}
