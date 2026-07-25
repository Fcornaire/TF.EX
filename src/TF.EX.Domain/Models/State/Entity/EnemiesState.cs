using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.Domain.Models.State.Entity
{
    [MessagePackObject]
    public class EnemiesState
    {
        [Key(0)]
        public ICollection<QuestSpawnPortal> QuestSpawnPortals { get; set; } = new List<QuestSpawnPortal>();

        [Key(1)]
        public ICollection<Slime> Slimes { get; set; } = new List<Slime>();

        [Key(2)]
        public ICollection<Bat> Bats { get; set; } = new List<Bat>();

        [Key(3)]
        public ICollection<EnemyAttack> EnemyAttacks { get; set; } = new List<EnemyAttack>();

        [Key(4)]
        public DarkPortalsSequence DarkPortalsSequence { get; set; }
    }
}
