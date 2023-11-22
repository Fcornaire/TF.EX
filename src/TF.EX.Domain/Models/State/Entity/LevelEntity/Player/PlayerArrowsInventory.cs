using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
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
