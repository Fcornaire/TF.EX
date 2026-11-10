using MessagePack;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.Domain.Models.Entity.LevelEntity.Arrows;
using TF.State.Domain.Models.Entity.LevelEntity.Background;
using TF.State.Domain.Models.Entity.LevelEntity.Chest;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;
using TF.State.Domain.Models.Entity.LevelEntity.Player;

namespace TF.State.Domain.Models.Entity
{
    [MessagePackObject]
    public class Entities
    {
        [Key(0)]
        public ICollection<Player> Players { get; set; } = [];
        [Key(1)]
        public ICollection<PlayerCorpse> PlayerCorpses { get; set; } = [];
        [Key(2)]
        public ICollection<Arrow> Arrows { get; set; } = [];
        [Key(4)]
        public ICollection<Pickup> Pickups { get; set; } = [];
        [Key(5)]
        public ICollection<Lantern> Lanterns { get; set; } = [];
        [Key(6)]
        public ICollection<Chain> Chains { get; set; } = [];
        [Key(7)]
        public ICollection<JumpPad> JumpPads { get; set; } = [];
        [Key(9)]
        public ICollection<CrackedPlatform> CrackedPlatforms { get; set; } = [];
        [Key(10)]
        public ICollection<Explosion> Explosions { get; set; } = [];
        [Key(11)]
        public ICollection<BGTorch> BGTorches { get; set; } = [];
        [Key(12)]
        public Spikeball Spikeball { get; set; } //TODO: are there map with multiple spikeballs?

        [Key(14)]
        public HUD.HUD Hud { get; set; } = new HUD.HUD();

        [Key(15)]
        public OrbLogic.OrbLogic OrbLogic { get; set; } = new OrbLogic.OrbLogic();
        [Key(16)]
        public ICollection<CrackedWall> CrackedWalls { get; set; } = [];
        [Key(17)]
        public ICollection<BGMushroom> BGMushrooms { get; set; } = [];
        [Key(18)]
        public ICollection<MovingPlatform> MovingPlatforms { get; set; } = [];

        [Key(19)]
        public ICollection<Brambles> Brambles { get; set; } = [];

        [Key(20)]
        public ICollection<Icicle> Icicles { get; set; } = [];

        [Key(21)]
        public ICollection<SnowClump> SnowClumps { get; set; } = [];

        [Key(22)]
        public ICollection<SwitchBlock> SwitchBlocks { get; set; } = [];

        [Key(23)]
        public SwitchBlockControl SwitchBlockControl { get; set; }

        [Key(24)]
        public ICollection<ShiftBlock> ShiftBlocks { get; set; } = [];

        [Key(25)]
        public ICollection<ProximityBlock> ProximityBlocks { get; set; } = [];

        [Key(26)]
        public ICollection<MoonGlassBlock> MoonGlassBlocks { get; set; } = [];

        [Key(27)]
        public ICollection<GhostPlatform> GhostPlatforms { get; set; } = [];

        [Key(28)]
        public ICollection<LoopPlatform> LoopPlatforms { get; set; } = [];

        [Key(29)]
        public ICollection<RotatePlatform> RotatePlatforms { get; set; } = [];

        [Key(30)]
        public ICollection<SensorBlock> SensorBlocks { get; set; } = [];

        [Key(31)]
        public ICollection<CrumbleBlock> CrumbleBlocks { get; set; } = [];

        [Key(32)]
        public Dictionary<int, RoundData> RoundData { get; set; } = new Dictionary<int, RoundData>();

        [Key(33)]
        public ICollection<Prism> Prisms { get; set; } = [];

        [Key(34)]
        public ICollection<CrumbleWall> CrumbleWalls { get; set; } = [];

        [Key(35)]
        public ICollection<TeamReviver> TeamRevivers { get; set; } = [];

        [Key(36)]
        public ICollection<LevelEntity.Player.PlayerGhost> PlayerGhosts { get; set; } = new List<LevelEntity.Player.PlayerGhost>();

        [Key(37)]
        public EnemiesState Enemies { get; set; } = new EnemiesState();

        [Key(38)]
        public ICollection<Dummy> Dummies { get; set; } = [];

        [Key(39)]
        public ICollection<DummyHead> DummyHeads { get; set; } = [];

        [Key(40)]
        public ICollection<Hat> Hats { get; set; } = [];

        [Key(41)]
        public ICollection<Ice> Ices { get; set; }

        [Key(42)]
        public ICollection<HeavyPhysicsObject> HeavyPhysicsObjects { get; set; } = [];
    }
}
