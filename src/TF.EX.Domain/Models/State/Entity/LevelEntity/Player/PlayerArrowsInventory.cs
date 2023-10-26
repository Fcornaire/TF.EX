using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class PlayerArrowsInventory
    {
        [Key(0)]
        public int Normal { get; set; }
    }


}
