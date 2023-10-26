using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Chest
{
    [MessagePackObject]
    public class Pickup
    {
        [Key(0)]
        public bool IsCollidable { get; set; }

        [Key(1)]
        public PickupState Type { get; set; }

        [Key(2)]
        public Vector2f Position { get; set; }

        [Key(3)]
        public Vector2f TargetPosition { get; set; }

        [Key(4)]
        public float SineCounter { get; set; }

        [Key(5)]
        public float TweenTimer { get; set; }

        [Key(6)]
        public float CollidableTimer { get; set; }

        [Key(7)]
        public int PlayerIndex { get; set; }

        [Key(8)]
        public double ActualDepth { get; set; }

        [Key(9)]
        public bool MarkedForRemoval { get; set; }

        [Key(10)]
        public Sprite<int> Sprite { get; set; }

        [Key(11)]
        public bool FinishedUnpack { get; set; }

        public static Pickup Empty()
        {
            return new Pickup
            {
                Position = new Vector2f { X = -1, Y = -1 },
                TargetPosition = new Vector2f { X = -1, Y = -1 },
                PlayerIndex = -1,
                Type = PickupState.Arrows,
                TweenTimer = -1,
                CollidableTimer = -1,
                IsCollidable = false,
                SineCounter = -1,
            };
        }
    }
}
