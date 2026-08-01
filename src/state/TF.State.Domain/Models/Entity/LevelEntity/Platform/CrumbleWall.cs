using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class CrumbleWall
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public bool Destroyed { get; set; }

        [Key(3)]
        public bool Shaking { get; set; }

        [Key(4)]
        public bool IsCollidable { get; set; }

        [Key(5)]
        public Alarm BreakAlarm { get; set; }
    }
}
