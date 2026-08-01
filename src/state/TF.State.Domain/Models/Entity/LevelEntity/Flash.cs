using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class Flash
    {
        [Key(0)]
        public bool IsFlashing { get; set; }

        [Key(1)]
        public float FlashCounter { get; set; }

        [Key(2)]
        public float FlashInterval { get; set; }
    }
}
