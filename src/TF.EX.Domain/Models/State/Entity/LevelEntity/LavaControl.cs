using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class LavaControl
    {
        [Key(0)]
        public LavaMode Mode { get; set; }
        [Key(1)]
        public int OwnerIndex { get; set; }
        [Key(2)]
        public float TargetCounter { get; set; }
        [Key(3)]
        public float Target { get; set; }
        [Key(4)]
        public Lava[] Lavas { get; set; }
    }
}
