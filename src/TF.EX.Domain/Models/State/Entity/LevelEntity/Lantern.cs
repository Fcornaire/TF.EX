using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Lantern
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public Vector2f PositionCounter { get; set; }

        [Key(2)]
        public bool IsFalling { get; set; }

        [Key(3)]
        public bool IsDead { get; set; }

        [Key(4)]
        public float VSpeed { get; set; }

        [Key(5)]
        public bool IsCollidable { get; set; }

        [Key(6)]
        public double ActualDepth { get; set; }


        public static Lantern Empty()
        {
            return new Lantern
            {
                Position = new Vector2f { X = -1, Y = -1 },
                PositionCounter = new Vector2f { X = -1, Y = -1 },
                IsFalling = false,
                IsDead = false,
                VSpeed = 0f,
                IsCollidable = false,
            };
        }
    }
}
