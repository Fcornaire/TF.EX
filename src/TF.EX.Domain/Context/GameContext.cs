using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.HUD;
using TF.EX.Domain.Models.State.Orb;
using TF.EX.Domain.Utils;

namespace TF.EX.Domain.Context
{
    public interface IGameContext //TODO: internal context so that only services can access this API
    {
        void AddStuckArrow(Vector2f arrowPosition, TowerFall.Platform platform);
        Dictionary<Vector2f, TowerFall.Platform> GetPlatoformStuckArrows();
        void UpdateCurrentInputs(IEnumerable<TowerFall.InputState> inputs);
        void UpdatePolledInput(TowerFall.InputState input);
        TowerFall.InputState GetPolledInput();
        TowerFall.InputState GetCurrentInput(int characterIndex);

        Session GetSession();
        void UpdateSession(Session session);
        Orb GetOrb();
        void SaveOrb(Orb orb);
        void ClearOrb();
        void SetSeed(int seed);

        int GetSeed();
        Rng GetRng();
        void UpdateRng(Rng rng);
        void InitializeReplay(int id);
        void AddRecord(GameState gameState, bool shouldSwapPlayer);
        void RemovePredictedRecords(int frame);
        Replay GetReplay();
        void LoadReplay(Replay replay);
        Record GetCurrentReplayFrame(int frame);
        List<TowerFall.InputState> GetCurrentInputs();
        Dictionary<int, double> GetGamePlayerLayerActualDepthLookup();
        void SaveGamePlayerLayerActualDepthLookup(Dictionary<int, double> toSave);
        void ResetGamePlayLayerActualDepthLookup();
        void ResetReplay();
        IEnumerable<BackgroundElement> GetBackgroundElements();
        void UpdateBackgroundElements(IEnumerable<BackgroundElement> backgroundElements);

        int GetLocalPlayerIndex();
        int GetRemotePlayerIndex();

        bool ShouldSwapPlayer();
        PlayerDraw GetPlayerDraw();
        void SetPlayersIndex(int playerDraw);

        void ResetPlayersIndex();
        HUD GetHUDState();
        void UpdateHUDState(HUD toLoad);
    }

    public class GameContext : IGameContext
    {
        private const int NUM_PLAYER = 2; //TODO: variable

        private readonly Dictionary<Vector2f, TowerFall.Platform> StuckArrows_Platforms; //Save Stuck arrow instead of serialising it TODO: clear when running a new netplay session
        private readonly AttributeManager<TowerFall.InputState> CurrentInputs;
        private TowerFall.InputState PolledInput;
        private Session Session;
        private Orb Orb;
        private Rng _rng = Rng.Default;
        private Replay _replay;
        private Dictionary<int, double> _gamePlayerLayerActualDepthLookup = new Dictionary<int, double>();
        private IEnumerable<BackgroundElement> _backgroundElements = new List<BackgroundElement>();
        private HUD _hudState;

        private int _localPlayerIndex = -1;
        private int _remotePlayerIndex = -1;

        public GameContext()
        {
            StuckArrows_Platforms = new Dictionary<Vector2f, TowerFall.Platform>();
            PolledInput = new TowerFall.InputState();
            CurrentInputs = new AttributeManager<TowerFall.InputState>(() => EmptyInput(), NUM_PLAYER);
            Session = new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                IsEnding = false,
                Miasma = Miasma.Default(),
                RoundStarted = false
            };
            Orb = Orb.Default;
            _hudState = new HUD();
        }

        public void AddStuckArrow(Vector2f arrowPosition, TowerFall.Platform platform)
        {
            if (!StuckArrows_Platforms.ContainsKey(arrowPosition))
            {
                StuckArrows_Platforms.Add(arrowPosition, platform);
            }
        }

        public Dictionary<Vector2f, TowerFall.Platform> GetPlatoformStuckArrows()
        {
            return StuckArrows_Platforms;
        }

        private TowerFall.InputState EmptyInput() { return new TowerFall.InputState(); }

        public void UpdateCurrentInputs(IEnumerable<TowerFall.InputState> inputs)
        {
            CurrentInputs.Update(inputs);
        }

        public void UpdatePolledInput(TowerFall.InputState input)
        {
            PolledInput = input;
        }

        public TowerFall.InputState GetCurrentInput(int characterIndex)
        {
            if (CurrentInputs.Get().Count == 0)
            {
                return EmptyInput();
            }
            return CurrentInputs[characterIndex];
        }

        public TowerFall.InputState GetPolledInput()
        {
            return PolledInput;
        }

        public Session GetSession()
        {
            return Session;
        }

        public void UpdateSession(Session session)
        {
            Session = session;
        }

        public Orb GetOrb()
        {
            return Orb;
        }

        public void SaveOrb(Orb orb)
        {
            Orb = orb;
        }

        public void ClearOrb()
        {
            Orb = Orb.Default;
        }

        public void SetSeed(int seed)
        {
            if (_rng.IsDefault())
            {
                _rng = new Rng(seed);
            }
        }

        public int GetSeed()
        {
            return _rng.Seed;
        }

        public Rng GetRng()
        {
            return _rng;
        }

        public void UpdateRng(Rng rng)
        {
            _rng = new Rng(rng.Seed);
            _rng.Gen_type = rng.Gen_type.ToList();
        }

        public void InitializeReplay(int towerId)
        {
            if (_replay == null)
            {
                _replay = new Replay
                {
                    Informations = new Info
                    {
                        Id = towerId,
                        PlayerDraw = PlayerDraw.Unkown,
                    },
                };
            }
        }

        public void AddRecord(GameState gameState, bool shouldSwapPlayer) //TODO: 2nd parameter might be useless
        {
            var gs = gameState;
            var inputs = CurrentInputs.Get().Select(input => input.ToModel()).ToList();

            _replay.Record.Add(new Record
            {
                GameState = gs,
                Inputs = inputs,
            });
        }

        public void RemovePredictedRecords(int frame)
        {
            _replay.Record.RemoveAll(rec => rec.GameState.Frame > frame);
        }

        public Replay GetReplay()
        {
            return _replay;
        }

        public void LoadReplay(Replay replay)
        {
            _replay = replay;
        }

        public Record GetCurrentReplayFrame(int frame)
        {
            return _replay.Record.Find(rec => rec.GameState.Frame == frame);
        }

        public List<TowerFall.InputState> GetCurrentInputs()
        {
            return CurrentInputs.Get();
        }

        public Dictionary<int, double> GetGamePlayerLayerActualDepthLookup()
        {
            var copy = new Dictionary<int, double>();

            foreach (var kvp in _gamePlayerLayerActualDepthLookup)
            {
                copy.Add(kvp.Key, kvp.Value);
            }

            return copy;
        }

        public void SaveGamePlayerLayerActualDepthLookup(Dictionary<int, double> toSave)
        {
            _gamePlayerLayerActualDepthLookup.Clear();

            foreach (var kvp in toSave)
            {
                _gamePlayerLayerActualDepthLookup.Add(kvp.Key, kvp.Value);
            }
        }

        public void ResetGamePlayLayerActualDepthLookup()
        {
            _gamePlayerLayerActualDepthLookup.Clear();
        }

        public void ResetReplay()
        {
            _replay = null;
        }

        public IEnumerable<BackgroundElement> GetBackgroundElements()
        {
            return _backgroundElements;
        }

        public void UpdateBackgroundElements(IEnumerable<BackgroundElement> backgroundElements)
        {
            _backgroundElements = backgroundElements.ToList();
        }

        public int GetLocalPlayerIndex()
        {
            if (_localPlayerIndex == -1)
            {
                _localPlayerIndex = GGRSFFI.netplay_local_player_handle();
            }

            return _localPlayerIndex;
        }

        public int GetRemotePlayerIndex()
        {
            if (_remotePlayerIndex == -1)
            {
                _remotePlayerIndex = GGRSFFI.netplay_remote_player_handle();
            }

            return _remotePlayerIndex;
        }

        public bool ShouldSwapPlayer()
        {
            return GetLocalPlayerIndex() != 0;
        }

        public PlayerDraw GetPlayerDraw()
        {
            return (PlayerDraw)GetLocalPlayerIndex();
        }

        /// <summary>
        /// Only to use in replay mode
        /// </summary>
        /// <param name="playerDraw"></param>
        public void SetPlayersIndex(int playerDraw)
        {
            _localPlayerIndex = playerDraw;

            if (playerDraw == 0)
            {
                _remotePlayerIndex = 1;
            }
            else
            {
                _remotePlayerIndex = 0;
            }
        }

        public void ResetPlayersIndex()
        {
            _localPlayerIndex = -1;
            _remotePlayerIndex = -1;
        }

        public HUD GetHUDState()
        {
            return new HUD
            {
                VersusStart = _hudState.VersusStart,
                VersusRoundResults = _hudState.VersusRoundResults,
            };
        }

        public void UpdateHUDState(HUD toLoad)
        {
            _hudState = new HUD
            {
                VersusStart = toLoad.VersusStart,
                VersusRoundResults = toLoad.VersusRoundResults,
            };
        }
    }
}
