using Microsoft.Xna.Framework.Audio;
using TF.State.Domain.Models;
using TF.State.Domain.Models.Entity;
using TF.State.Domain.Models.Entity.HUD;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.Domain.Models.Entity.LevelEntity.Chest;
using TF.State.Domain.Models.Entity.LevelEntity.Platform;
using TF.State.Domain.Randomness;

namespace TF.State.Domain.Context
{
    public interface IStateContext
    {
        Session GetSession();
        void UpdateSession(Session session);

        void SetSeed(int seed);
        int GetSeed();
        Rng GetRng();
        void UpdateRng(Rng rng);
        System.Random GetGameplayRandom();

        Dictionary<int, double> GetGamePlayerLayerActualDepthLookup();
        void SaveGamePlayerLayerActualDepthLookup(Dictionary<int, double> toSave);
        void ResetGamePlayLayerActualDepthLookup();

        HUD GetHUDState();
        void UpdateHUDState(HUD toLoad);

        void AddesiredSfx(SFX toPlay);
        ICollection<SFX> GetDesiredSfx();
        void LoadDesiredSfx(IEnumerable<SFX> sFXes);
        void ClearDesiredSfx();
        ICollection<SoundEffectPlaying> GetCurrentSfxs();
        void ClearSfxs();
        void AddSoundEffect(SoundEffect data, string filename);
        string GetSoundEffectName(SoundEffect data);
        int GetLastRollbackFrame();
        void UpdateLastRollbackFrame(int frame);

        void AddBramblesState(float frameCounter, IEnumerable<MovingPlatform> movingPlatformsStates, Vector2f spreadOrigin);
        IEnumerable<BramblesStartingState> GetBramblesStartingState();
        void LoadBramblesStartingState(IEnumerable<BramblesStartingState> states);

        void UpdateRoundChests(int roundIndex, List<Chest> chests);
        void UpdateRoundOrbs(int roundIndex, List<Orb> orbs);
        void UpdateRoundLavaControl(int roundIndex, LavaControl lavaControl);
        Dictionary<int, RoundData> GetRoundData();
        void LoadRoundData(Dictionary<int, RoundData> roundData);

        void Reset();
    }

    internal class StateContext : IStateContext
    {
        private Session Session;
        private int _seed = -1;
        private readonly DeterministicRandom _gameplayRandom = new DeterministicRandom(0);
        private Dictionary<int, double> _gamePlayerLayerActualDepthLookup = new Dictionary<int, double>();
        private HUD _hudState;
        private ICollection<SFX> _desiredSfxs = new List<SFX>();
        private ICollection<SoundEffectPlaying> _currentSfxs = new List<SoundEffectPlaying>();
        private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>();
        private ICollection<BramblesStartingState> bramblesStates = new List<BramblesStartingState>();
        private Dictionary<int, RoundData> roundDataPerRound = new Dictionary<int, RoundData>();
        private int _lastRollbackFrame = 0;

        public StateContext()
        {
            Session = NewSession();
            _hudState = new HUD();
        }

        private static Session NewSession() => new Session
        {
            RoundEndCounter = Constants.INITIAL_END_COUNTER,
            GhostWaitCounter = Constants.INITIAL_END_COUNTER,
            IsEnding = false,
            Miasma = Miasma.Default(),
            RoundStarted = false
        };

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

            foreach (var kvp in roundDataPerRound.OrderBy(entry => entry.Key))
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

        public void Reset()
        {
            bramblesStates.Clear();
            roundDataPerRound.Clear();

            ResetGamePlayLayerActualDepthLookup();
            ClearSfxs();

            UpdateSession(NewSession());
        }
    }
}
