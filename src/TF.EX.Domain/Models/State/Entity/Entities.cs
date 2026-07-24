using MessagePack;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Background;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
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
        [Key(4)]
        public ICollection<Pickup> Pickups { get; set; } = new List<Pickup>();
        [Key(5)]
        public ICollection<Lantern> Lanterns { get; set; } = new List<Lantern>();
        [Key(6)]
        public ICollection<Chain> Chains { get; set; } = new List<Chain>();
        [Key(7)]
        public ICollection<JumpPad> JumpPads { get; set; } = new List<JumpPad>();
        [Key(9)]
        public ICollection<CrackedPlatform> CrackedPlatforms { get; set; } = new List<CrackedPlatform>();
        [Key(10)]
        public ICollection<Explosion> Explosions { get; set; } = new List<Explosion>();
        [Key(11)]
        public ICollection<BGTorch> BGTorches { get; set; } = new List<BGTorch>();
        [Key(12)]
        public Spikeball Spikeball { get; set; } //TODO: are there map with multiple spikeballs?

        [Key(14)]
        public HUD.HUD Hud { get; set; } = new HUD.HUD();

        [Key(15)]
        public OrbLogic.OrbLogic OrbLogic { get; set; } = new OrbLogic.OrbLogic();
        [Key(16)]
        public ICollection<CrackedWall> CrackedWalls { get; set; } = new List<CrackedWall>();
        [Key(17)]
        public ICollection<BGMushroom> BGMushrooms { get; set; } = new List<BGMushroom>();
        [Key(18)]
        public ICollection<MovingPlatform> MovingPlatforms { get; set; } = new List<MovingPlatform>();

        [Key(19)]
        public ICollection<Brambles> Brambles { get; set; } = new List<Brambles>();

        [Key(20)]
        public ICollection<Icicle> Icicles { get; set; } = new List<Icicle>();

        [Key(21)]
        public ICollection<SnowClump> SnowClumps { get; set; } = new List<SnowClump>();

        [Key(22)]
        public ICollection<SwitchBlock> SwitchBlocks { get; set; } = new List<SwitchBlock>();

        [Key(23)]
        public SwitchBlockControl SwitchBlockControl { get; set; }

        [Key(24)]
        public ICollection<ShiftBlock> ShiftBlocks { get; set; } = new List<ShiftBlock>();

        [Key(25)]
        public ICollection<ProximityBlock> ProximityBlocks { get; set; } = new List<ProximityBlock>();

        [Key(26)]
        public ICollection<MoonGlassBlock> MoonGlassBlocks { get; set; } = new List<MoonGlassBlock>();

        [Key(27)]
        public ICollection<GhostPlatform> GhostPlatforms { get; set; } = new List<GhostPlatform>();

        [Key(28)]
        public ICollection<LoopPlatform> LoopPlatforms { get; set; } = new List<LoopPlatform>();

        [Key(29)]
        public ICollection<RotatePlatform> RotatePlatforms { get; set; } = new List<RotatePlatform>();

        [Key(30)]
        public ICollection<SensorBlock> SensorBlocks { get; set; } = new List<SensorBlock>();

        [Key(31)]
        public ICollection<CrumbleBlock> CrumbleBlocks { get; set; } = new List<CrumbleBlock>();

        [Key(32)]
        public Dictionary<int, RoundData> RoundData { get; set; } = new Dictionary<int, RoundData>();

        [Key(33)]
        public ICollection<Prism> Prisms { get; set; } = new List<Prism>();

        [Key(34)]
        public ICollection<CrumbleWall> CrumbleWalls { get; set; } = new List<CrumbleWall>();
    }
}
