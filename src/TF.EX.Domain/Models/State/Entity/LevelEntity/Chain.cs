using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class Chain
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public float CannotHitCounter { get; set; }

        [Key(2)]
        public float[] Speeds { get; set; }

        [Key(3)]
        public float[] Rotations { get; set; }

        [Key(4)]
        public Vector2f[] Bottoms { get; set; }

        [Key(5)]
        public double ActualDepth { get; set; }

        public static Chain Empty()
        {
            return new Chain
            {
                Position = new Vector2f { X = -1, Y = -1 },
                CannotHitCounter = -1,
                Speeds = new float[0],
                Rotations = new float[0],
                Bottoms = new Vector2f[0],
            };
        }
    }
}
