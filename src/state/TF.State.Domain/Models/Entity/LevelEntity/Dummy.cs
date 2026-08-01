using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Dummy
    {
        [Key(0)]
        public Vector2f Position { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }

        [Key(2)]
        public int Facing { get; set; }

        [Key(3)]
        public bool Dead { get; set; }

        [Key(4)]
        public bool IsCollidable { get; set; }

        [Key(5)]
        public bool IsVisible { get; set; }
    }
}
