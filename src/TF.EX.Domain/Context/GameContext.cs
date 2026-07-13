using Microsoft.Xna.Framework.Audio;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Entity.HUD;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Utils;

namespace TF.EX.Domain.Context
{
    public interface IGameContext //TODO: internal context so that only services can access this API
    {
        void UpdateCurrentInputs(IEnumerable<Input> inputs);
        void UpdatePolledInput(Input input);
        Input GetPolledInput();
        Input GetCurrentInput(int characterIndex);

        Session GetSession();
        void UpdateSession(Session session);
        void SetSeed(int seed);

        int GetSeed();
        Rng GetRng();
        void UpdateRng(Rng rng);
        void InitializeReplay(int id, GameData gameData = null, ICollection<CustomMod> mods = null);
        void AddRecord(GameState gameState, bool shouldSwapPlayer);
        void RemovePredictedRecords(int frame);
        Replay GetReplay();
        void LoadReplay(Replay replay);
        Record GetCurrentReplayFrame(int frame);
        List<Input> GetCurrentInputs();
        Dictionary<int, double> GetGamePlayerLayerActualDepthLookup();
        void SaveGamePlayerLayerActualDepthLookup(Dictionary<int, double> toSave);
        void ResetGamePlayLayerActualDepthLookup();
        void ResetReplay();
        int GetLocalPlayerIndex();
        int GetRemotePlayerIndex();

        bool ShouldSwapPlayer();
        PlayerDraw GetPlayerDraw();
        void SetPlayersIndex(int playerDraw);

        void ResetPlayersIndex();
        HUD GetHUDState();
        void UpdateHUDState(HUD toLoad);
        void AddesiredSfx(SFX toPlay);
        ICollection<SoundEffectPlaying> GetCurrentSfxs();
        ICollection<SFX> GetDesiredSfx();
        int GetLastRollbackFrame();
        void LoadDesiredSfx(IEnumerable<SFX> sFXes);
        void UpdateLastRollbackFrame(int frame);
        void AddSoundEffect(SoundEffect data, string filename);
        void ClearDesiredSfx();
        IEnumerable<(int, string)> GetArchers();
        IEnumerable<(int, Player)> GetPlayers();
        void AddArcher(int index, Player player);
        void ResetArcherSelections();
        void RemoveArcher(int playerIndex);
        void ClearSfxs();
        string GetSoundEffectName(SoundEffect data);
    }

    internal class GameContext : IGameContext
    {
        private const int NUM_PLAYER = 2; //TODO: variable

        private readonly AttributeManager<Input> CurrentInputs;
        private Input PolledInput;
        private Session Session;
        private Rng _rng = new Rng();
        private Replay _replay;
        private Dictionary<int, double> _gamePlayerLayerActualDepthLookup = new Dictionary<int, double>();
        private HUD _hudState;
        private ICollection<SFX> _desiredSfxs = new List<SFX>();
        private ICollection<SoundEffectPlaying> _currentSfxs = new List<SoundEffectPlaying>();
        private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>(); //TODO: should be cached instead
        private Dictionary<int, Player> ArcherSelections = new Dictionary<int, Player>();
        private int _lastRollbackFrame = 0;

        private int _localPlayerIndex = -1;
        private int _remotePlayerIndex = -1;

        public GameContext()
        {
            PolledInput = new Input();
            CurrentInputs = new AttributeManager<Input>(EmptyInput, NUM_PLAYER);
            Session = new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                IsEnding = false,
                Miasma = Miasma.Default(),
                RoundStarted = false
            };
            _hudState = new HUD();
        }

        private Input EmptyInput() { return new Input(); }

        public void UpdateCurrentInputs(IEnumerable<Input> inputs)
        {
            CurrentInputs.Update(inputs);
        }

        public void UpdatePolledInput(Input input)
        {
            PolledInput = input;
        }

        public Input GetCurrentInput(int characterIndex)
        {
            if (characterIndex < 0)
            {
                return EmptyInput();
            }

            if (CurrentInputs.Get().Count == 0)
            {
                return EmptyInput();
            }

            if (characterIndex >= CurrentInputs.Get().Count)
            {
                return EmptyInput();
            }

            return CurrentInputs[characterIndex];
        }

        public Input GetPolledInput()
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

        public void SetSeed(int seed)
        {
            _rng = new Rng
            {
                Seed = seed,
                Gen_type = new List<RngGenType>()
            };
        }

        public int GetSeed()
        {
            return _rng.Seed;
        }

        public Rng GetRng()
        {
            var rng = new Rng { Seed = _rng.Seed, Gen_type = _rng.Gen_type.ToList() };
            return rng;
        }

        public void UpdateRng(Rng rng)
        {
            _rng = new Rng { Seed = rng.Seed, Gen_type = rng.Gen_type.ToList() };
        }

        public void InitializeReplay(int towerId, GameData gameData = null, ICollection<CustomMod> mods = null)
        {
            if (_replay == null)
            {
                _replay = new Replay
                {
                    Informations = new ReplayInfo
                    {
                        Id = towerId,
                        PlayerDraw = PlayerDraw.Unkown,
                        Version = ServiceCollections.CurrentReplayVersion,
                        Mods = mods.ToList() ?? new List<CustomMod>(),
                    },
                };

                if (gameData != null)
                {
                    _replay.Informations.Variants = gameData.Variants ?? new List<string>();
                    _replay.Informations.Mode = (TF.EX.Domain.Models.Modes)gameData.Mode;
                    _replay.Informations.VersusMatchLength = gameData.MatchLength;
                }
            }
        }

        public void AddRecord(GameState gameState, bool shouldSwapPlayer) //TODO: 2nd parameter might be useless
        {
            var gs = gameState;
            var inputs = CurrentInputs.Get().ToList();

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

        public List<Input> GetCurrentInputs()
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
                VersusStart = new VersusStart
                {
                    CoroutineState = _hudState.VersusStart.CoroutineState,
                    TweenState = _hudState.VersusStart.TweenState,
                },
                VersusRoundResults = new VersusRoundResults
                {
                    CoroutineState = _hudState.VersusRoundResults.CoroutineState,
                }
            };
        }

        public void UpdateHUDState(HUD toLoad)
        {
            _hudState = new HUD
            {
                VersusStart = new VersusStart
                {
                    CoroutineState = toLoad.VersusStart.CoroutineState,
                    TweenState = toLoad.VersusStart.TweenState,
                },
                VersusRoundResults = new VersusRoundResults
                {
                    CoroutineState = toLoad.VersusRoundResults.CoroutineState,
                }
            };
        }

        public void AddesiredSfx(SFX toPlay)
        {
            if (!_desiredSfxs.Any(sfx => sfx.Name == toPlay.Name && toPlay.Frame == sfx.Frame))
            {
                _desiredSfxs.Add(toPlay);
            }
        }

        public ICollection<SFX> GetDesiredSfx()
        {
            return _desiredSfxs;
        }

        public void LoadDesiredSfx(IEnumerable<SFX> sFXes)
        {
            var toLoad = sFXes.ToList();
            toLoad.ForEach(sfx =>
            {
                if (sfx != null && !string.IsNullOrEmpty(sfx.Name) && _soundEffects.ContainsKey(sfx.Name))
                {
                    sfx.Data = _soundEffects[sfx.Name];
                }

            });

            _desiredSfxs = toLoad;
        }

        public void ClearDesiredSfx()
        {
            _desiredSfxs.Clear();
        }

        public void UpdateLastRollbackFrame(int frame)
        {
            _lastRollbackFrame = frame;
        }

        public void AddSoundEffect(SoundEffect data, string filename)
        {
            if (!_soundEffects.ContainsKey(filename))
            {
                _soundEffects.Add(filename, data);
            }
        }

        public ICollection<SoundEffectPlaying> GetCurrentSfxs()
        {
            return _currentSfxs;
        }

        public int GetLastRollbackFrame()
        {
            return _lastRollbackFrame;
        }

        public IEnumerable<(int, string)> GetArchers()
        {
            return ArcherSelections.Select(kvp => (kvp.Key, $"{kvp.Value.ArcherIndex}-{kvp.Value.ArcherAltIndex}"));
        }

        public void AddArcher(int index, Player player)
        {
            if (!ArcherSelections.ContainsKey(index))
            {
                ArcherSelections.Add(index, player);
                return;
            }

            //FortRise.Logger.Log($"Archer already selected for player {index}");
        }

        public void ResetArcherSelections()
        {
            ArcherSelections.Clear();
        }

        public void RemoveArcher(int playerIndex)
        {
            ArcherSelections.Remove(playerIndex);
        }

        public IEnumerable<(int, Player)> GetPlayers()
        {
            return ArcherSelections.Select(kvp => (kvp.Key, kvp.Value));
        }

        public void ClearSfxs()
        {
            ClearDesiredSfx();
            _currentSfxs.Clear();
            _soundEffects.Clear();
        }

        public string GetSoundEffectName(SoundEffect data)
        {
            return _soundEffects.FirstOrDefault(kvp => kvp.Value == data).Key;
        }
    }
}
