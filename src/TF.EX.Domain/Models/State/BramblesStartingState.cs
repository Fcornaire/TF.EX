using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class BramblesStartingState
    {
        [Key(0)]
        public float FrameCounter { get; set; }
        [Key(1)]
        public ICollection<MovingPlatform> MovingPlatforms { get; set; } = new List<MovingPlatform>();
    }
}
