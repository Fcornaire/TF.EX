using TF.EX.Domain.Models.State.Monocle;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class CrackedPlatform
    {
        public Vector2f Position { get; set; }
        public Vector2f PositionCounter { get; set; }
        public double ActualDepth { get; set; }
        public bool IsCollidable { get; set; }
        public CrackedPlatformStates State { get; set; }
        public Alarm Shake { get; set; }
        public Alarm Respawn { get; set; }
        public Flash Flash { get; set; }
    }
}
