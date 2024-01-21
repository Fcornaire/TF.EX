using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class CrackedPlatform
    {
        [Key(0)]
        public Vector2f Position { get; set; }
        [Key(1)]
        public Vector2f PositionCounter { get; set; }
        [Key(2)]
        public double ActualDepth { get; set; }
        [Key(3)]
        public bool IsCollidable { get; set; }
        [Key(4)]
        public CrackedPlatformStates State { get; set; }
        [Key(5)]
        public Alarm Shake { get; set; }
        [Key(6)]
        public Alarm Respawn { get; set; }
        [Key(7)]
        public Flash Flash { get; set; }
    }
}
