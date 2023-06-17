using static TowerFall.LavaControl;

namespace TF.EX.Domain.Models.State.LevelEntity
{
    public class LavaControl
    {
        public LavaMode Mode { get; set; }
        public int OwnerIndex { get; set; }
        public float TargetCounter { get; set; }
        public float Target { get; set; }
        public Lava[] Lavas { get; set; }



        public static LavaControl Default => new LavaControl
        {
            Mode = LavaMode.Pickup,
            OwnerIndex = 0,
            TargetCounter = -100,
            Target = -100,
            Lavas = new Lava[0]
        };


        public bool IsDefault() => TargetCounter == -100 && Target == -100;
    }
}
