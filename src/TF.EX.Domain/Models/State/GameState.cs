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

        public IEnumerable<MatchStats> MatchStats { get; set; } = new List<MatchStats>();
        public IEnumerable<SFXState> SFXs { get; set; } = new List<SFXState>();

        public Vector2f ScreenOffset { get; set; } = new Vector2f(-1, -1);

        public int Frame { get; set; }
    }

    public class SFXState
    {
        public string Name { get; set; }
        public int Frame { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public float Pan { get; set; }
    }
}
