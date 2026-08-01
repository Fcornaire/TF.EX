using MessagePack;
using TF.State.Domain.Models.Component;

namespace TF.State.Domain.Models.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class ProximityBlock
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public bool IsCollidable { get; set; }

        [Key(2)]
        public bool Transitioning { get; set; }

        [Key(3)]
        public Tween DisappearTween { get; set; }

        [Key(4)]
        public Tween AppearTween { get; set; }
    }
}
