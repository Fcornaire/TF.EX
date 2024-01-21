using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Background
{
    [MessagePackObject]
    public class BGTorch
    {
        [Key(0)]
        public bool LightVisible { get; set; }

        [Key(1)]
        public double ActualDepth { get; set; }
    }
}
