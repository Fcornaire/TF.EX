using MessagePack;
using TF.State.Domain.Models;

namespace TF.State.Domain.Models.Entity.LevelEntity.Arrows
{
    [MessagePackObject]
    public class BrambleArrow : Arrow
    {
        [Key(23)]
        public bool CanDie { get; set; }
        [Key(24)]
        public bool IsUsed { get; set; }
        [Key(25)]
        public BrambleSpreadState BrambleSpread { get; set; }
    }
}
