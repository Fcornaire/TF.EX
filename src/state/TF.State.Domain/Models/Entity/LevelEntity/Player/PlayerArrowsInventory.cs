using MessagePack;
using TF.State.Domain.Models.Entity.LevelEntity.Arrows;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerArrowsInventory
    {
        [Key(0)]
        public IEnumerable<ArrowTypes> Arrows { get; set; }

        [Key(1)]
        public IEnumerable<ArrowTypes> SortSet { get; set; }
    }
}
