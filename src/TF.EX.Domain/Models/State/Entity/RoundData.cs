using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;

namespace TF.EX.Domain.Models.State.Entity
{
    /// <summary>
    /// Per-round entities
    /// </summary>
    [MessagePackObject]
    public class RoundData
    {
        [Key(0)]
        public List<Chest> Chests { get; set; } = new List<Chest>();

        [Key(1)]
        public List<Orb> Orbs { get; set; } = new List<Orb>();

        [Key(2)]
        public LavaControl LavaControl { get; set; }
    }
}
