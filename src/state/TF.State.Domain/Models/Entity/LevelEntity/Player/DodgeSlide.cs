using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class DodgeSlide
    {
        [Key(0)]
        public bool IsDodgeSliding { get; set; }

        [Key(1)]
        public bool WasDodgeSliding { get; set; }
    }
}
