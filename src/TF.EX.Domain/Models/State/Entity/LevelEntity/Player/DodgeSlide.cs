using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
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
