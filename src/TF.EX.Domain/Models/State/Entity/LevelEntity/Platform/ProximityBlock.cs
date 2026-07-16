using MessagePack;
using TF.EX.Domain.Models.State.Component;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
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
