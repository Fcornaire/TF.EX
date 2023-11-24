using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerCorpse
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public Vector2f PositionCounter { get; set; }

        [Key(2)]
        public int PlayerIndex { get; set; }

        [Key(3)]
        public int KillerIndex { get; set; }

        [Key(4)]
        public Vector2f Speed { get; set; }

        [Key(5)]
        public Facing Facing { get; set; }

        [Key(6)]
        public float FallSpriteCounter { get; set; }

        [Key(7)]
        public bool Pinned { get; set; }

        [Key(8)]
        public ArrowCushion ArrowCushion { get; set; }

        [Key(9)]
        public double ActualDepth { get; set; }
    }

    [MessagePackObject]
    public class ArrowCushionData
    {
        [Key(0)]
        public double ActualDepth;

        [Key(1)]
        public Vector2f Offset;

        [Key(2)]
        public float Rotation;
    }
}
