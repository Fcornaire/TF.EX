namespace TF.EX.Domain.Models.State
{
    public class GameState
    {
        public Entity.Entity Entities { get; set; } = new Entity.Entity();

        public Layer.Layer Layer { get; set; } = new Layer.Layer();

        public Session Session { get; set; } = new Session
        {
            RoundEndCounter = Constants.INITIAL_END_COUNTER,
            IsEnding = false,
            Miasma = Miasma.Default(),
            RoundStarted = false,
            IsDone = false
        };
        public Rng Rng { get; set; } = new Rng(-1);

        public bool IsLevelFrozen { get; set; } = false;
        public RoundLogic RoundLogic { get; set; } = new RoundLogic();

        public IEnumerable<TowerFall.MatchStats> MatchStats { get; set; } = new List<TowerFall.MatchStats>();
        public IEnumerable<SFX> SFXs { get; set; } = new List<SFX>();

        public Vector2f ScreenOffset { get; set; } = new Vector2f(-1, -1);

        public int Frame { get; set; }
    }
}
