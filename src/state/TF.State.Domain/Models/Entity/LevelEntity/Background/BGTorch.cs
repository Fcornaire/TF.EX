using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Background
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
