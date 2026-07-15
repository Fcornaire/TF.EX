using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class ShiftBlock
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public Vector2f MoveFrom { get; set; }

        [Key(2)]
        public Vector2f MoveTo { get; set; }

        [Key(3)]
        public float MoveLerp { get; set; }

        [Key(4)]
        public int State { get; set; }

        [Key(5)]
        public Counter Counter { get; set; }

        [Key(6)]
        public double ActualDepth { get; set; }
    }
}
