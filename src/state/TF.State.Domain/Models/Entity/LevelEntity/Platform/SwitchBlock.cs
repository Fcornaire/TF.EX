using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Platform
{
    [MessagePackObject]
    public class SwitchBlock
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public bool On { get; set; }

        [Key(2)]
        public bool IsCollidable { get; set; }
    }
}
