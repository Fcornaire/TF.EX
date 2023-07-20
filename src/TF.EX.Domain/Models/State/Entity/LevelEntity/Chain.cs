namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class Chain
    {
        public Vector2f Position { get; set; }
        public float CannotHitCounter { get; set; }
        public float[] Speeds { get; set; }
        public float[] Rotations { get; set; }
        public Vector2f[] Bottoms { get; set; }
        public double ActualDepth { get; set; }

        public static Chain Empty()
        {
            return new Chain
            {
                Position = new Vector2f { x = -1, y = -1 },
                CannotHitCounter = -1,
                Speeds = new float[0],
                Rotations = new float[0],
                Bottoms = new Vector2f[0],
            };
        }
    }
}
