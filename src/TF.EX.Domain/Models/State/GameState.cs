using MessagePack;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class GameState
    {
        [Key(0)]
        public Entity.Entities Entities { get; set; } = new Entity.Entities();

        [Key(1)]
        public Layer.Layer Layer { get; set; } = new Layer.Layer();

        [Key(2)]
        public Session Session { get; set; } = new Session
        {
            RoundEndCounter = Constants.INITIAL_END_COUNTER,
            IsEnding = false,
            Miasma = Miasma.Default(),
            RoundStarted = false,
            IsDone = false
        };

        [Key(3)]
        public Rng Rng { get; set; } = new Rng { Seed = -1, Gen_type = new List<RngGenType>() };

        [Key(4)]
        public bool IsLevelFrozen { get; set; } = false;

        [Key(5)]
        public RoundLogic RoundLogic { get; set; } = new RoundLogic();

        [Key(6)]
        public IEnumerable<MatchStats> MatchStats { get; set; } = new List<MatchStats>();

        [Key(7)]
        public IEnumerable<SFXState> SFXs { get; set; } = new List<SFXState>();

        [Key(8)]
        public Vector2f ScreenOffset { get; set; } = new Vector2f { X = -1, Y = -1 };

        [Key(9)]
        public Dictionary<string, string> AdditionnalData { get; set; } = new Dictionary<string, string>();

        [Key(10)]
        public int Frame { get; set; }
    }

    [MessagePackObject]
    public class SFXState
    {
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public int Frame { get; set; }
        [Key(2)]
        public float Volume { get; set; }
        [Key(3)]
        public float Pitch { get; set; }
        [Key(4)]
        public float Pan { get; set; }
    }
}
