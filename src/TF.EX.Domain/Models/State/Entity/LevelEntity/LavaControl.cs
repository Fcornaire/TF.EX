using static TowerFall.LavaControl;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    public class LavaControl
    {
        public LavaMode Mode { get; set; }
        public int OwnerIndex { get; set; }
        public float TargetCounter { get; set; }
        public float Target { get; set; }
        public Lava[] Lavas { get; set; }
    }
}
