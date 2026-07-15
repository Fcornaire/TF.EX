using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Platform
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
