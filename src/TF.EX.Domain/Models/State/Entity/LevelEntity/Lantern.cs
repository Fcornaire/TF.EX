namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Lantern
    {
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public bool IsFalling { get; set; }
        public bool IsDead { get; set; }
        public float VSpeed { get; set; }
        public bool IsCollidable { get; set; }
        public double ActualDepth { get; set; }


        public static Lantern Empty()
        {
            return new Lantern
            {
                Position = new Vector2f { x = -1, y = -1 },
                PositionCounter = new Vector2f { x = -1, y = -1 },
                IsFalling = false,
                IsDead = false,
                VSpeed = 0f,
                IsCollidable = false,
            };
        }
    }
}
