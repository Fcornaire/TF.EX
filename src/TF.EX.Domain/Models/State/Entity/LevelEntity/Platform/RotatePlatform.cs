using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class RotatePlatform
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
        public float CurrentAngle { get; set; }
    }
}
