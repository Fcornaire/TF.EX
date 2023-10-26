using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
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
