using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
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
