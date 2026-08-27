using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
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
        private int _pendingHoldFrame = -1;
        private int _primingFrame;
        private Level _drivenLevel;
        private Level _outgoingLevel;

        private const int NoResync = -1;

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
                EffectiveVariants());
        }

        private static string[] EffectiveVariants()
        {
            var settings = (TowerFall.TFGame.Instance?.Scene as TowerFall.Level)?.Session?.MatchSettings ?? TowerFall.MainMenu.VersusMatchSettings;
            var variants = settings?.Variants;

            if (variants == null)
            {
                return [];
            }

            return
            [
                .. variants.Variants.Where(variant => variant.Value).Select(variant => variant.Title),
                .. variants.CustomVariants.Where(pair => pair.Value.Value).Select(pair => pair.Value.Title),
            ];
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
                archers.Select(a => a.Seat).ToArray(),
                archers.Select(a => a.Index).ToArray(),
                archers.Select(a => (int)a.Type).ToArray(),
                archers.Select(a => a.HasWon).ToArray(),
                archers.Select(a => a.Score).ToArray(),
                archers.Select(a => a.NetplayName).ToArray());

            ReplayApi.Current.SetArcherCustomIds(archers.Select(a => a.CustomArcherId ?? "").ToArray());
            ReplayApi.Current.SetArcherSkinIds(archers.Select(a => a.SkinArcherId ?? "").ToArray());

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
            CustomComponent.Notification.IsDeferedOn = true;

            string failure;

            try
            {
                failure = ReplayApi.Current.StartPlayback(replayFilename);
            }
            finally
            {
                CustomComponent.Notification.IsDeferedOn = false;
            }

            if (failure != null)
            {
                CustomComponent.Notification.ClearDeferred();
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

            ReplayIntroPacing.Reset();
            ReplayIntroPacing.ConfirmLatched = !RecordedIntroIsHumanPaced();

            ApplyNetplayGameMode();

            _divergenceReported = false;
            _outOfRecordsReported = false;
            _renderDiffReported = false;
            _encodingDiffReported = false;
            _primingDiffReported = false;
            _lastDivergenceLogFrame = 0;
            _playbackPrimed = false;
            _primingFrame = 0;
            _pendingHoldFrame = -1;
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

            if (!matchSettings.ApplyNetplayMode(applyVariantRules: false))
            {
                _logger.LogWarning("[Replay] Netplay game mode is not registered");
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

            int frame;

            if (_pendingHoldFrame >= 0)
            {
                frame = _pendingHoldFrame;
                _pendingHoldFrame = -1;
            }
            else
            {
                frame = ReplayApi.Current.ConsumeNextRecordFrame();
            }

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

                SpawnRoundPlayers(level);

                _primingFrame = frame;
            }

            var recordedState = ReplayApi.Current.GetStateAtFrame(frame);

            ExFlags.CurrentFrame = frame;
            StateApi.Current.SetCurrentFrame(frame);

            if (level != null)
            {
                DynamicData.For(level).Set("FrameCounter", (float)frame);
            }

            if (priming)
            {
                PrimePlayback(frame, recordedState);
            }

            if (ReplayApi.Current?.IsTakeoverInProgress() != true)
            {
                var slip = VerifyAgainstRecordedState(frame, recordedState);

                if (slip > 0)
                {
                    for (int skipped = 0; skipped < slip; skipped++)
                    {
                        var next = ReplayApi.Current.ConsumeNextRecordFrame();

                        if (next < 0)
                        {
                            break;
                        }

                        frame = next;
                    }

                    recordedState = ReplayApi.Current.GetStateAtFrame(frame);
                    ExFlags.CurrentFrame = frame;
                    StateApi.Current.SetCurrentFrame(frame);

                    if (level != null)
                    {
                        DynamicData.For(level).Set("FrameCounter", (float)frame);
                    }
                }
                else if (slip < 0)
                {
                    _pendingHoldFrame = frame;
                }
            }

            var inputs = _pendingHoldFrame >= 0
                ? ReplayApi.Current.GetInputsAtFrame(frame)
                : ReplayApi.Current.GetInputsAtFrame(frame + 1) ?? ReplayApi.Current.GetInputsAtFrame(frame);

            if (inputs != null)
            {
                _inputService.UpdateCurrent(inputs.ToInputs());
            }

            if (!ReplayIntroPacing.ConfirmLatched
                && (AnyButtonPressed(inputs)
                    || (recordedState != null && StateApi.Current.IsRoundStarted(recordedState))))
            {
                ReplayIntroPacing.ConfirmLatched = true;
            }
        }

        private static bool RecordedIntroIsHumanPaced()
        {
            for (int frame = 0; ; frame++)
            {
                var inputs = ReplayApi.Current.GetInputsAtFrame(frame);

                if (inputs == null)
                {
                    return false;
                }

                var state = ReplayApi.Current.GetStateAtFrame(frame);

                if (state != null && StateApi.Current.IsRoundStarted(state))
                {
                    return false;
                }

                if (AnyButtonPressed(inputs))
                {
                    return true;
                }
            }
        }

        private static bool AnyButtonPressed(int[] flat)
        {
            for (int seat = 0; seat < Models.InputCodec.SeatCount(flat); seat++)
            {
                var offset = Models.InputCodec.Offset(seat);

                for (int button = Models.InputCodec.JumpCheck; button <= Models.InputCodec.ArrowPressed; button++)
                {
                    if (flat[offset + button] != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
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
                _logger.LogDebug("[Replay] Primed at frame {frame} from start {start}", frame, start);
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

        private void SpawnRoundPlayers(Level level)
        {
            if (level?.Session?.RoundLogic is IRoundPlayerSpawner spawner)
            {
                spawner.SpawnRoundPlayersOnly();

                level.UpdateEntityLists();
            }
            else
            {
                _logger.LogWarning("[Replay] No round player spawner at playback start");
            }
        }

        private void PrimePlayback(int frame, byte[] recordedState)
        {
            var level = TFGame.Instance?.Scene as Level;

            if (recordedState != null && !StateApi.Current.LoadGameStateBytes(recordedState))
            {
                _logger.LogWarning("[Replay] Could not restore the first recorded frame; playback starts from the freshly loaded level");
            }

            if (level != null
                && level.Session?.MatchSettings?.Mode != TowerFall.Modes.Trials
                && level.Get<VersusStart>() == null
                && !(recordedState != null
                && StateApi.Current.IsRoundStarted(recordedState)))
            {
                level.Add(new VersusStart(level.Session));
            }
        }

        private int VerifyAgainstRecordedState(int frame, byte[] recordedState)
        {
            if (recordedState == null)
            {
                return 0;
            }

            byte[] liveBytes;

            try
            {
                liveBytes = StateApi.Current.GetGameStateBytesForRecording();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Replay] Could not capture the live state at frame {frame}", frame);
                return 0;
            }

            if (liveBytes == null)
            {
                return 0;
            }

            if (liveBytes.AsSpan().SequenceEqual(recordedState))
            {
                _divergenceReported = false;

                return 0;
            }

            if (frame <= _primingFrame)
            {
                if (!_primingDiffReported)
                {
                    _primingDiffReported = true;
                    _logger.LogWarning("[Replay] Primed state differs from its own record at frame {frame}", frame);
                }

                return 0;
            }

            if (_divergenceReported)
            {
                if (frame - _lastDivergenceLogFrame >= DivergenceLogInterval)
                {
                    _lastDivergenceLogFrame = frame;
                    Log(ServiceCollections.ResolveSkinOverlayService().HasReplaySkins, "[Replay] Playback diverged at frame {frame} ?", frame);
                }

                return 0;
            }

            var slip = DetectOffset(frame, liveBytes);

            if (slip != 0)
            {
                _logger.LogWarning("[Replay] Playback slipped {offset} frame at {frame}", slip, frame);

                return slip;
            }

            var resync = TryResyncAtRoundBoundary(frame, recordedState, liveBytes);

            if (resync != NoResync)
            {
                return resync;
            }

            LogFirstDivergence(frame, recordedState, liveBytes);

            return 0;
        }


        private int TryResyncAtRoundBoundary(int frame, byte[] recordedState, byte[] liveBytes)
        {
            var liveStarted = StateApi.Current.IsRoundStarted(liveBytes);
            var recordStarted = StateApi.Current.IsRoundStarted(recordedState);

            if (liveStarted && recordStarted)
            {
                var previous = ReplayApi.Current.GetStateAtFrame(frame - 1);

                if (previous == null || StateApi.Current.IsRoundStarted(previous) || !StateApi.Current.LoadGameStateBytes(recordedState))
                {
                    return NoResync;
                }

                _logger.LogWarning("[Replay] Round start differs from the record at {frame}", frame);

                return 0;
            }

            if (liveStarted == recordStarted)
            {
                return NoResync;
            }

            for (int offset = 1; offset <= 3; offset++)
            {
                var neighbour = ReplayApi.Current.GetStateAtFrame(frame + offset);

                if (neighbour == null)
                {
                    return NoResync;
                }

                if (StateApi.Current.IsRoundStarted(neighbour) == liveStarted && StateApi.Current.LoadGameStateBytes(neighbour))
                {
                    return offset;
                }
            }

            return NoResync;
        }

        private int DetectOffset(int frame, byte[] liveBytes)
        {
            foreach (var offset in new[] { 1, -1, 2, -2, 3, -3 })
            {
                var neighbourState = ReplayApi.Current.GetStateAtFrame(frame + offset);

                if (neighbourState == null)
                {
                    continue;
                }

                if (StateApi.Current.StateMatchesWithFrame(neighbourState, liveBytes, frame))
                {
                    return offset;
                }
            }

            return 0;
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
                        _logger.LogWarning("[Replay] Encoding-only difference at frame {frame}, states are equal but bytes are not.{detail}", frame, detail);
                    }

                    return;
                }

                if (kind == "render-derived")
                {
                    if (!_renderDiffReported)
                    {
                        _renderDiffReported = true;
                        _logger.LogWarning("[Replay] Render-derived sprite state differs at frame {frame}: {detail}", frame, detail);
                    }

                    return;
                }

                _divergenceReported = true;
                _lastDivergenceLogFrame = frame;

                var hasReplaySkins = ServiceCollections.ResolveSkinOverlayService().HasReplaySkins;

                Log(hasReplaySkins,
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
                        Log(hasReplaySkins, "[Replay] Live state actually matches record {frame} , playback is offset by {offset}, not desynced", frame + offset, offset);
                    }
                    else
                    {
                        var neighbourDiff = StateApi.Current.ClassifyStateDiff(liveBytes, neighbourState);
                        Log(hasReplaySkins, "[Replay] Live vs record {frame} ({kind}): {detail}", frame + offset, neighbourDiff[0], neighbourDiff[1]);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Replay] Divergence detected at frame {frame} but the diff itself failed ?", frame);
            }
        }

        private void Log(bool quiet, string message, params object[] args)
        {
            if (quiet)
            {
                _logger.LogDebug(message, args);
            }
            else
            {
                _logger.LogWarning(message, args);
            }
        }

        public int GetFrame() => ReplayApi.Current.PlaybackFrame;

        public int GetLoadedReplayMode() => ReplayApi.Current.GetLoadedReplayMode();

        public byte[] GetCurrentStateBytes() => ReplayApi.Current.GetStateAtFrame(ReplayApi.Current.PlaybackFrame);


        public void GoTo(int numberOfFrames) => ReplayApi.Current.SeekTo(ReplayApi.Current.PlaybackFrame + numberOfFrames);
    }
}
