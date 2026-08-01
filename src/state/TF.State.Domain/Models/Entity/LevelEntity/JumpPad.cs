using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class JumpPad
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public bool IsOn { get; set; }
    }
}
