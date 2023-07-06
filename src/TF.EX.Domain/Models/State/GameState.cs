using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Models.State.LevelEntity;
using TF.EX.Domain.Models.State.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Player;

namespace TF.EX.Domain.Models.State
{
    public class GameState
    {
        public Layer.Layer Layer { get; set; } = new Layer.Layer();
        public List<Player.Player> Players { get; set; } = new List<Player.Player>();
        public List<PlayerCorpse> PlayerCorpses { get; set; } = new List<PlayerCorpse>();
        public List<Arrow> Arrows { get; set; } = new List<Arrow>();
        public List<LevelEntity.Chest.Chest> Chests { get; set; } = new List<LevelEntity.Chest.Chest>();
        public List<Pickup> Pickups { get; set; } = new List<Pickup>();
        public List<Lantern> Lanterns { get; set; } = new List<Lantern>();
        public List<Chain> Chains { get; set; } = new List<Chain>();
        public Session Session { get; set; } = new Session
        {
            RoundEndCounter = Constants.INITIAL_END_COUNTER,
            IsEnding = false,
            Miasma = Miasma.Default(),
            RoundStarted = false,
            IsDone = false
        };
        public Orb.Orb Orb { get; set; } = new Orb.Orb();
        public Rng Rng { get; set; } = new Rng(-1);
        public Dictionary<int, double> GamePlayerLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

        public HUD.HUD Hud { get; set; } = new HUD.HUD();

        public EventLog.EventLog EventLogs { get; set; } = new EventLog.EventLog();

        public RoundLevels RoundLevels { get; set; } = new RoundLevels();

        public bool IsLevelFrozen { get; set; } = false;
        public RoundLogic RoundLogic { get; set; } = new RoundLogic();
        public int Frame { get; set; }
    }
}
