using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class LoopPlatform
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public Vector2f PositionCounter { get; set; }

        [Key(3)]
        public float SinkAmount { get; set; }

        [Key(4)]
        public Vector2f MoveAdd { get; set; }

        [Key(5)]
        public bool IsWaiting { get; set; }
    }
}
