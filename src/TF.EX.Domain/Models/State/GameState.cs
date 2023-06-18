using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Models.State.LevelEntity;
using TF.EX.Domain.Models.State.LevelEntity.Chest;

namespace TF.EX.Domain.Models.State
{
    public class GameState
    {
        public List<BackgroundElement> BackgroundElements { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
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
            RoundStarted = false
        };
        public Orb.Orb Orb { get; set; } = new Orb.Orb();
        public Rng Rng { get; set; } = new Rng(-1);
        public Dictionary<int, double> GamePlayerLayerActualDepthLookup { get; set; } = new Dictionary<int, double>();

        public HUD.HUD Hud { get; set; } = new HUD.HUD();
        public int Frame { get; set; }
    }
}
