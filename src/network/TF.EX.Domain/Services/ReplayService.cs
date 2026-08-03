using Microsoft.Extensions.Logging;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Domain.Context;
using TowerFall;

namespace TF.EX.Domain.Services
{
    /// <summary>
    /// EX's adapter onto TF.Replay
    /// </summary>
    public class ReplayService : IReplayService
    {
        private const string DriverName = "TF.EX";
        private const int DivergenceLogInterval = 300;
        private const int PrimeScanLimit = 30;

        private readonly IInputService _inputService;
        private readonly INetplayManager _netplayManager;
        private readonly ILogger _logger;

        private bool _divergenceReported;
        private int _lastDivergenceLogFrame;
        private bool _outOfRecordsReported;
        private bool _renderDiffReported;
        private bool _encodingDiffReported;
        private bool _primingDiffReported;
        private bool _playbackPrimed;
        private int _primingFrame;
        private Level _drivenLevel;
        private Level _outgoingLevel;

        public ReplayService(IInputService inputService, INetplayManager netplayManager, ILogger logger)
        {
            _inputService = inputService;
            _netplayManager = netplayManager;
            _logger = logger;
        }

        public void RegisterPlaybackCallbacks() => ReplayApi.Current.SetPlaybackStartedCallback(OnPlaybackStarting);

        public void Initialize(Models.WebSocket.GameData gameData = null, ICollection<Models.WebSocket.CustomMod> mods = null)
        {
            var mode = gameData?.Mode ?? (int)TowerFall.MainMenu.VersusMatchSettings.Mode;

            foreach (var mod in mods ?? new List<Models.WebSocket.CustomMod>())
            {
                ReplayApi.Current.AddRecordingMod(mod.Name, mod.Data?.Keys.ToArray(), mod.Data?.Values.ToArray());
            }

            ReplayApi.Current.BeginRecording(
                CurrentTowerId(),
                mode,
                gameData?.MatchLength ?? 2,
                (gameData?.Variants ?? new List<string>()).ToArray());
        }

        private static int CurrentTowerId()
        {
            var settings = (TowerFall.TFGame.Instance?.Scene as TowerFall.Level)?.Session?.MatchSettings ?? TowerFall.MainMenu.VersusMatchSettings;

            return settings?.LevelSystem?.ID.X ?? 0;
        }

        public void AddRecord(byte[] state, int frame)
        {
            ReplayApi.Current.AddRecord(state, _inputService.GetCurrentInputs().ToFlatInputs(), frame);
        }

        public void RemovePredictedRecords(int frame) => ReplayApi.Current.RemovePredictedRecords(frame);

        private static int[] CurrentTeams()
        {
            var settings = (TowerFall.TFGame.Instance?.Scene as TowerFall.Level)?.Session?.MatchSettings
                ?? TowerFall.MainMenu.VersusMatchSettings;

            if (settings?.Teams == null)
            {
                return [];
            }

            var teams = new int[4];

            for (int seat = 0; seat < teams.Length; seat++)
            {
                teams[seat] = (int)settings.Teams[seat];
            }

            return teams;
        }

        public void Export()
        {
            ReplayApi.Current.SetLocalSeat(_netplayManager.LocalSeat);
            ReplayApi.Current.SetSeed(StateApi.Current.GetSeed());
            ReplayApi.Current.SetPlayerCount(_netplayManager.GetNumPlayers());

            var archers = _netplayManager.GetArchersInfo();
            ReplayApi.Current.SetArchersFlat(
                archers.Select(a => a.Index).ToArray(),
                archers.Select(a => (int)a.Type).ToArray(),
                archers.Select(a => a.HasWon).ToArray(),
                archers.Select(a => a.Score).ToArray(),
                archers.Select(a => a.NetplayName).ToArray());

            ReplayApi.Current.SetArcherTeams(CurrentTeams());

            ReplayApi.Current.Export();
        }

        public void Reset()
        {
            _divergenceReported = false;
            _outOfRecordsReported = false;
            _renderDiffReported = false;
            _encodingDiffReported = false;
            _primingDiffReported = false;
            _lastDivergenceLogFrame = 0;
            _playbackPrimed = false;
            _primingFrame = 0;
            ReplayApi.Current.ResetRecording();
        }

        public async Task<string> LoadAndStart(string replayFilename, string currentSong = "")
        {
            var failure = ReplayApi.Current.StartPlayback(replayFilename);

            if (failure != null)
            {
                Monocle.Music.Play(currentSong);
                return failure;
            }

            return await Task.FromResult<string>(null);
        }

        private void OnPlaybackStarting(int localSeat)
        {
            StateApi.Current.SetFrameDriver(DriverName);

            _netplayManager.SetLocalSeat(localSeat);
            _netplayManager.SetReplayMode();

            ExFlags.IsCaptureActive = true;
            ExFlags.IsReplayMode = true;
            ExFlags.CurrentFrame = 0;
            ExFlags.Push();

            ApplyNetplayGameMode();

            _divergenceReported = false;
            _outOfRecordsReported = false;
            _renderDiffReported = false;
            _encodingDiffReported = false;
            _primingDiffReported = false;
            _lastDivergenceLogFrame = 0;
            _playbackPrimed = false;
            _primingFrame = 0;
            _outgoingLevel = _drivenLevel;
        }

        private void ApplyNetplayGameMode()
        {
            var matchSettings = TowerFall.MainMenu.VersusMatchSettings;

            if (matchSettings == null)
            {
                return;
            }

            if (matchSettings.Mode == TowerFall.Modes.Trials || ReplayApi.Current?.GetLoadedReplayMode() == (int)TowerFall.Modes.Trials)
            {
                _logger.LogInformation("[Replay] Trials replay: leaving the vanilla round logic alone");
                return;
            }

            if (!matchSettings.ApplyNetplayMode())
            {
                _logger.LogWarning("[Replay] Netplay game mode is not registered - playback would run vanilla round logic and diverge");
            }
        }

        public void RunFrame()
        {
            var level = TFGame.Instance?.Scene as Level;

            if (level != null && ReferenceEquals(level, _outgoingLevel))
            {
                return;
            }

            _outgoingLevel = null;
            _drivenLevel = level;

            var frame = ReplayApi.Current.ConsumeNextRecordFrame();

            if (frame < 0)
            {
                if (!_outOfRecordsReported)
                {
                    _outOfRecordsReported = true;
                    _logger.LogDebug("[Replay] Ran out of records at playback frame {frame}", ReplayApi.Current.PlaybackFrame);
                }

                return;
            }

            var priming = !_playbackPrimed;

            if (priming)
            {
                _playbackPrimed = true;
                frame = SkipToSpawnedRound(frame);
                _primingFrame = frame;
            }

            var recordedState = ReplayApi.Current.GetStateAtFrame(frame);

            ExFlags.CurrentFrame = frame;
            StateApi.Current.SetCurrentFrame(frame);

            if (priming)
            {
                PrimePlayback(frame, recordedState);
            }

            VerifyAgainstRecordedState(frame, recordedState);

            var inputs = ReplayApi.Current.GetInputsAtFrame(frame + 1) ?? ReplayApi.Current.GetInputsAtFrame(frame);

            if (inputs != null)
            {
                _inputService.UpdateCurrent(inputs.ToInputs());
            }
        }

        private int SkipToSpawnedRound(int frame)
        {
            var start = frame;

            for (int scanned = 0; scanned < PrimeScanLimit; scanned++)
            {
                if (HasPlayers(ReplayApi.Current.GetStateAtFrame(frame)))
                {
                    break;
                }

                var next = ReplayApi.Current.ConsumeNextRecordFrame();

                if (next < 0)
                {
                    break;
                }

                frame = next;
            }

            if (frame != start)
            {
                _logger.LogDebug("[Replay] Primed at frame {frame}", frame, start, frame - 1);
            }

            return frame;
        }

        private bool HasPlayers(byte[] state)
        {
            if (state == null)
            {
                return false;
            }

            try
            {
                return StateApi.Current.DescribePlayers(state).Length > 0;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "[Replay] Could not read the players of the record being primed");

                return true;
            }
        }

        /// <summary>
        /// A live match idles the level while GGRS synchronizes,playback make the round exactly where the session-established callback did
        /// </summary>
        private void PrimePlayback(int frame, byte[] recordedState)
        {
            var level = TFGame.Instance?.Scene as Level;

            if (level?.Session?.RoundLogic is IRoundPlayerSpawner spawner)
            {
                spawner.SpawnRoundPlayersOnly();

                level.UpdateEntityLists();
            }
            else
            {
                _logger.LogWarning("[Replay] No round player spawner at playback start");
            }

            if (recordedState != null && !StateApi.Current.LoadGameStateBytes(recordedState))
            {
                _logger.LogWarning("[Replay] Could not restore the first recorded frame; playback starts from the freshly loaded level");
            }

            if (level != null
                && level.Session?.MatchSettings?.Mode != TowerFall.Modes.Trials
                && level.Get<VersusStart>() == null)
            {
                level.Add(new VersusStart(level.Session));
            }
        }

        private void VerifyAgainstRecordedState(int frame, byte[] recordedState)
        {
            if (recordedState == null)
            {
                return;
            }

            byte[] liveBytes;

            try
            {
                liveBytes = StateApi.Current.GetGameStateBytesForRecording();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Replay] Could not capture the live state at frame {frame}", frame);
                return;
            }

            if (liveBytes == null || liveBytes.AsSpan().SequenceEqual(recordedState))
            {
                return;
            }

            if (frame <= _primingFrame)
            {
                if (!_primingDiffReported)
                {
                    _primingDiffReported = true;
                    _logger.LogWarning("[Replay] Primed state differs from its own record at frame {frame}",frame);
                }

                return;
            }

            if (_divergenceReported)
            {
                if (frame - _lastDivergenceLogFrame >= DivergenceLogInterval)
                {
                    _lastDivergenceLogFrame = frame;
                    _logger.LogWarning("[Replay] Playback  diverged at frame {frame}", frame);
                }

                return;
            }

            LogFirstDivergence(frame, recordedState, liveBytes);
        }

        private void LogFirstDivergence(int frame, byte[] recordedState, byte[] liveBytes)
        {
            try
            {
                var classified = StateApi.Current.ClassifyStateDiff(liveBytes, recordedState);
                var kind = classified[0];
                var detail = classified[1];
                var round = classified[2];

                if (kind == "equal")
                {
                    return;
                }

                if (kind == "encoding-only")
                {
                    if (!_encodingDiffReported)
                    {
                        _encodingDiffReported = true;
                        _logger.LogWarning(
                            "[Replay] Encoding-only difference at frame {frame}, states are equal but bytes are not.{detail}",
                            frame, detail);
                    }

                    return;
                }

                if (kind == "render-derived")
                {
                    if (!_renderDiffReported)
                    {
                        _renderDiffReported = true;
                        _logger.LogWarning(
                            "[Replay] Render-derived sprite state differs at frame {frame}: {detail}",
                            frame, detail);
                    }

                    return;
                }

                _divergenceReported = true;
                _lastDivergenceLogFrame = frame;

                _logger.LogError(
                    "[Replay] PLAYBACK DIVERGED at frame {frame} (round {round}, live len {liveLen} vs recorded {recLen}), Live vs recorded: {detail}",
                    frame, round, liveBytes.Length, recordedState.Length, detail);

                foreach (var offset in new[] { -1, 1 })
                {
                    var neighbourState = ReplayApi.Current.GetStateAtFrame(frame + offset);

                    if (neighbourState == null)
                    {
                        continue;
                    }

                    if (StateApi.Current.StateMatchesWithFrame(neighbourState, liveBytes, frame))
                    {
                        _logger.LogError("[Replay] Live state actually matches record {frame} , playback is offset by {offset}, not desynced", frame + offset, offset);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Replay] Divergence detected at frame {frame} but the diff itself failed ?", frame);
            }
        }

        public int GetFrame() => ReplayApi.Current.PlaybackFrame;

        public int GetLoadedReplayMode() => ReplayApi.Current.GetLoadedReplayMode();

        public byte[] GetCurrentStateBytes() => ReplayApi.Current.GetStateAtFrame(ReplayApi.Current.PlaybackFrame);


        public void GoTo(int numberOfFrames) => ReplayApi.Current.SeekTo(ReplayApi.Current.PlaybackFrame + numberOfFrames);
    }
}
