using MessagePack;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.Domain.Models.Entity
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

        [Key(5)]
        public ICollection<ChaliceGhost> ChaliceGhosts { get; set; } = new List<ChaliceGhost>();

        [Key(6)]
        public ICollection<TechnoMage> TechnoMages { get; set; } = new List<TechnoMage>();

        [Key(7)]
        public ICollection<TechnoMissile> TechnoMissiles { get; set; } = new List<TechnoMissile>();
    }
}
