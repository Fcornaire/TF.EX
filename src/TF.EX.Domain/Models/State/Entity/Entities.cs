using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.Domain.Models.State.Entity
{
    [MessagePackObject]
    public class Entities
    {
        [Key(0)]
        public ICollection<Player> Players { get; set; } = new List<Player>();
        [Key(1)]
        public ICollection<PlayerCorpse> PlayerCorpses { get; set; } = new List<PlayerCorpse>();
        [Key(2)]
        public ICollection<Arrow> Arrows { get; set; } = new List<Arrow>();
        [Key(3)]
        public ICollection<Chest> Chests { get; set; } = new List<Chest>();
        [Key(4)]
        public ICollection<Pickup> Pickups { get; set; } = new List<Pickup>();
        [Key(5)]
        public ICollection<Lantern> Lanterns { get; set; } = new List<Lantern>();
        [Key(6)]
        public ICollection<Chain> Chains { get; set; } = new List<Chain>();
        [Key(7)]
        public ICollection<JumpPad> JumpPads { get; set; } = new List<JumpPad>();
        [Key(8)]
        public ICollection<Orb> Orbs { get; set; } = new List<Orb>();
        [Key(9)]
        public ICollection<CrackedPlatform> CrackedPlatforms { get; set; } = new List<CrackedPlatform>();
        [Key(10)]
        public ICollection<Explosion> Explosions { get; set; } = new List<Explosion>();
        [Key(11)]
        public ICollection<BGTorch> BGTorches { get; set; } = new List<BGTorch>();
        [Key(12)]
        public Spikeball Spikeball { get; set; } //TODO: are there map with multiple spikeballs?

        [Key(13)]
        public LavaControl LavaControl { get; set; }

        [Key(14)]
        public HUD.HUD Hud { get; set; } = new HUD.HUD();

        [Key(15)]
        public OrbLogic.OrbLogic OrbLogic { get; set; } = new OrbLogic.OrbLogic();
    }
}
