using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Hat
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f PositionCounter { get; set; }

        [Key(3)]
        public Vector2f Speed { get; set; }

        [Key(4)]
        public int PlayerIndex { get; set; }

        [Key(5)]
        public int HatState { get; set; }

        [Key(6)]
        public float Spin { get; set; }

        [Key(7)]
        public float SineCounter { get; set; }

        [Key(8)]
        public float SineRate { get; set; }

        [Key(9)]
        public float Rotation { get; set; }

        [Key(10)]
        public bool Flipped { get; set; }

        [Key(11)]
        public bool Pending { get; set; }

        [Key(12)]
        public float SineValue { get; set; }
    }
}
