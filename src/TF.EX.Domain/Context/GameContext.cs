using Microsoft.Xna.Framework.Audio;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Models;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Entity;
using TF.EX.Domain.Models.State.Entity.HUD;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Randomness;
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
        System.Random GetGameplayRandom();
        void InitializeReplay(int id, GameData gameData = null, ICollection<CustomMod> mods = null);
        void AddRecord(GameState gameState);
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
        void SetLocalSeat(int seat);
        TowerFall.PlayerInput GetLocalInput();
        void SetLocalInput(TowerFall.PlayerInput input);
        void SetInputLocked(bool locked);
        bool IsInputLocked();

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
        void AddBramblesState(float frameCounter, IEnumerable<MovingPlatform> movingPlatformsStates, Vector2f spreadOrigin);
        void Reset();
        IEnumerable<BramblesStartingState> GetBramblesStartingState();
        void LoadBramblesStartingState(IEnumerable<BramblesStartingState> states);
        void UpdateRoundChests(int roundIndex, List<Chest> chests);
        void UpdateRoundOrbs(int roundIndex, List<Orb> orbs);
        void UpdateRoundLavaControl(int roundIndex, LavaControl lavaControl);
        Dictionary<int, RoundData> GetRoundData();
        void LoadRoundData(Dictionary<int, RoundData> roundData);
    }

    internal class GameContext : IGameContext
    {
        private const int MAX_PLAYERS = 4;

        private readonly AttributeManager<Input> CurrentInputs;
        private Input PolledInput;
        private Session Session;
        private int _seed = -1;
        private readonly DeterministicRandom _gameplayRandom = new DeterministicRandom(0);
        private Replay _replay;
        private Dictionary<int, double> _gamePlayerLayerActualDepthLookup = new Dictionary<int, double>();
        private HUD _hudState;
        private ICollection<SFX> _desiredSfxs = new List<SFX>();
        private ICollection<SoundEffectPlaying> _currentSfxs = new List<SoundEffectPlaying>();
        private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>(); //TODO: should be cached instead
        private Dictionary<int, Player> ArcherSelections = new Dictionary<int, Player>();
        private ICollection<BramblesStartingState> bramblesStates = new List<BramblesStartingState>();
        private Dictionary<int, RoundData> roundDataPerRound = new Dictionary<int, RoundData>();
        private int _lastRollbackFrame = 0;

        private const int INPUT_LOCK_TIMEOUT_SECONDS = 5;

        private int _localPlayerIndex = -1;
        private TowerFall.PlayerInput _localInput;
        private DateTime _inputLockedUntil = DateTime.MinValue;

        public GameContext()
        {
            PolledInput = new Input();
            CurrentInputs = new AttributeManager<Input>(EmptyInput, MAX_PLAYERS);
            Session = new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                GhostWaitCounter = Constants.INITIAL_END_COUNTER,
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
            _seed = seed;
            _gameplayRandom.Seed(unchecked((ulong)seed));
        }

        public int GetSeed()
        {
            return _seed;
        }

        public Rng GetRng()
        {
            var rng = new Rng { Seed = _seed };
            rng.SetState(_gameplayRandom.Snapshot());
            return rng;
        }

        public void UpdateRng(Rng rng)
        {
            _seed = rng.Seed;
            _gameplayRandom.Restore(rng.ToState());
        }

        public System.Random GetGameplayRandom()
        {
            return _gameplayRandom;
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
                        LocalSeat = -1,
                        Version = ServiceCollections.CurrentReplayVersion,
                        Mods = mods?.ToList() ?? new List<CustomMod>(),
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

        public void AddRecord(GameState gameState)
        {
            if (_replay == null)
            {
                return;
            }

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
            _replay?.Record.RemoveAll(rec => rec.GameState.Frame > frame);
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
            return _replay?.Record.Find(rec => rec.GameState.Frame == frame);
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
            if (_localPlayerIndex != -1 && ServiceCollections.ResolveNetplayManager().IsReplayMode())
            {
                return _localPlayerIndex;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (TowerFall.TFGame.Instance?.Scene is TowerFall.MainMenu
                && !matchmakingService.GetOwnLobby().IsEmpty
                && !matchmakingService.IsSpectator())
            {
                return matchmakingService.GetLocalSeat();
            }

            var handle = GGRSFFI.netplay_local_player_handle();

            return handle >= 0 ? handle : matchmakingService.GetLocalSeat();
        }

        public TowerFall.PlayerInput GetLocalInput()
        {
            return _localInput;
        }

        public void SetLocalInput(TowerFall.PlayerInput input)
        {
            _localInput = input;
        }

        public void SetInputLocked(bool locked)
        {
            _inputLockedUntil = locked ? DateTime.UtcNow.AddSeconds(INPUT_LOCK_TIMEOUT_SECONDS) : DateTime.MinValue;
        }

        public bool IsInputLocked()
        {
            return DateTime.UtcNow < _inputLockedUntil;
        }

        public void SetLocalSeat(int seat)
        {
            _localPlayerIndex = seat;
        }

        public void ResetPlayersIndex()
        {
            _localPlayerIndex = -1;
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
            return ArcherSelections
                .Select(kvp => (kvp.Key, $"{kvp.Value.ArcherIndex}-{kvp.Value.ArcherAltIndex}"))
                .ToList();
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
            return ArcherSelections.Select(kvp => (kvp.Key, kvp.Value)).ToList();
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

        public void AddBramblesState(float frameCounter, IEnumerable<MovingPlatform> movingPlatformsStates, Vector2f spreadOrigin)
        {
            if (bramblesStates.Any(state => state.FrameCounter == frameCounter))
            {
                return;
            }

            bramblesStates.Add(new BramblesStartingState
            {
                FrameCounter = frameCounter,
                MovingPlatforms = movingPlatformsStates.ToList(),
                Position = spreadOrigin
            });
        }

        public void Reset()
        {
            bramblesStates.Clear();
            roundDataPerRound.Clear();

            ResetPlayersIndex();
            ResetArcherSelections();
            ResetGamePlayLayerActualDepthLookup();
            ResetReplay();
            ClearSfxs();

            UpdateSession(new Session
            {
                RoundEndCounter = Constants.INITIAL_END_COUNTER,
                GhostWaitCounter = Constants.INITIAL_END_COUNTER,
                IsEnding = false,
                Miasma = Miasma.Default(),
                RoundStarted = false
            });
        }

        public IEnumerable<BramblesStartingState> GetBramblesStartingState()
        {
            return bramblesStates.ToList();
        }

        public void LoadBramblesStartingState(IEnumerable<BramblesStartingState> states)
        {
            bramblesStates = states.ToList();
        }

        public void UpdateRoundChests(int roundIndex, List<Chest> chests)
        {
            GetOrCreateRound(roundIndex).Chests = chests;
        }

        public void UpdateRoundOrbs(int roundIndex, List<Orb> orbs)
        {
            GetOrCreateRound(roundIndex).Orbs = orbs;
        }

        public void UpdateRoundLavaControl(int roundIndex, LavaControl lavaControl)
        {
            GetOrCreateRound(roundIndex).LavaControl = lavaControl;
        }

        public Dictionary<int, RoundData> GetRoundData()
        {
            var snapshot = new Dictionary<int, RoundData>();
            foreach (var kvp in roundDataPerRound)
            {
                snapshot[kvp.Key] = new RoundData
                {
                    Chests = kvp.Value.Chests,
                    Orbs = kvp.Value.Orbs,
                    LavaControl = kvp.Value.LavaControl
                };
            }
            return snapshot;
        }

        public void LoadRoundData(Dictionary<int, RoundData> toLoad)
        {
            roundDataPerRound = new Dictionary<int, RoundData>();
            foreach (var kvp in toLoad)
            {
                roundDataPerRound[kvp.Key] = new RoundData
                {
                    Chests = kvp.Value.Chests,
                    Orbs = kvp.Value.Orbs,
                    LavaControl = kvp.Value.LavaControl
                };
            }
        }

        private RoundData GetOrCreateRound(int roundIndex)
        {
            if (!roundDataPerRound.TryGetValue(roundIndex, out var roundData))
            {
                roundData = new RoundData();
                roundDataPerRound[roundIndex] = roundData;
            }
            return roundData;
        }
    }
}
