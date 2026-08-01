using MessagePack;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;

namespace TF.State.Domain.Models
{
    [MessagePackObject]
    public class BramblesStartingState
    {
        [Key(0)]
        public float FrameCounter { get; set; }
        [Key(1)]
        public ICollection<MovingPlatform> MovingPlatforms { get; set; } = new List<MovingPlatform>();
        [Key(2)]
        public Vector2f Position { get; set; }
    }
}
