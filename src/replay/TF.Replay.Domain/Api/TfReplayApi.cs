using Microsoft.Extensions.Logging;
using TF.Replay.Domain.Models;
using TF.Replay.Domain.Ports;

namespace TF.Replay.Domain.Api
{
    public class TfReplayApi : ITfReplayApi
    {
        private readonly IReplayService _replayService;
        private readonly Func<Interop.IModCollections> _modCollections;

        private readonly List<ArcherInfo> _archers = new List<ArcherInfo>();
        private readonly List<CustomMod> _mods = new List<CustomMod>();

        private bool? _fixedTimeStepBeforePlayback;

        public string RecordDriver { get; private set; }
        public Action<int> PlaybackStarted { get; private set; }
        public Action PlaybackStopped { get; private set; }

        public TfReplayApi(IReplayService replayService, Func<Interop.IModCollections> modCollections)
        {
            _replayService = replayService;
            _modCollections = modCollections;
        }

        private Interop.ITfStateApi StateApi() => _modCollections?.Invoke()?.ResolveState();


        public int ApiVersion => 1;

        public string GetReplayFormatVersion() => ReplayVersionExtensions.GetLatest().ToString();

        public int InputStride => InputCodec.Stride;

        public void SetRecordDriver(string ownerModName) => RecordDriver = ownerModName;

        public string GetRecordDriver() => RecordDriver;

        public void AddRecordingMod(string name, string[] dataKeys, string[] dataValues)
        {
            var mod = new CustomMod { Name = name };

            if (dataKeys != null && dataValues != null)
            {
                for (int i = 0; i < dataKeys.Length && i < dataValues.Length; i++)
                {
                    mod.Data[dataKeys[i]] = dataValues[i];
                }
            }

            _mods.Add(mod);
        }

        public void BeginRecording(int towerId, int mode, int versusMatchLength, string[] variants)
        {
            _archers.Clear();
            _replayService.BeginRecording(towerId, mode, versusMatchLength,
                variants ?? Array.Empty<string>(), _mods.ToList());
            _mods.Clear();
        }

        public void AddRecord(byte[] state, int[] inputs, int frame) => _replayService.AddRecord(state, inputs, frame);

        public void RemovePredictedRecords(int frame) => _replayService.RemovePredictedRecords(frame);

        public string Export()
        {
            FlushArchers();
            return _replayService.Export();
        }

        public void ResetRecording()
        {
            _archers.Clear();
            _mods.Clear();
            _replayService.Reset();
        }

        public void SetLocalSeat(int seat) => _replayService.SetLocalSeat(seat);

        public void SetPlayerCount(int count) => _replayService.SetPlayerCount(count);

        public void SetSeed(int seed) => _replayService.SetSeed(seed);

        public void SetArcher(int seat, int archerIndex, int archerType, bool hasWon, int score, string name)
        {
            while (_archers.Count <= seat)
            {
                _archers.Add(new ArcherInfo());
            }

            _archers[seat] = new ArcherInfo
            {
                Index = archerIndex,
                Type = (ArcherTypes)archerType,
                HasWon = hasWon,
                Score = score,
                NetplayName = name,
            };

            FlushArchers();
        }

        public void SetArchersFlat(int[] seats, int[] indexes, int[] types, bool[] hasWon, int[] scores, string[] names)
        {
            _archers.Clear();

            for (int i = 0; i < (indexes?.Length ?? 0); i++)
            {
                _archers.Add(new ArcherInfo
                {
                    Seat = seats != null && i < seats.Length ? seats[i] : i,
                    Index = indexes[i],
                    Type = (ArcherTypes)types[i],
                    HasWon = hasWon[i],
                    Score = scores[i],
                    NetplayName = names[i],
                });
            }

            FlushArchers();
        }

        public void SetArcherCustomIds(string[] customArcherIds)
        {
            for (int i = 0; i < (customArcherIds?.Length ?? 0) && i < _archers.Count; i++)
            {
                _archers[i].CustomArcherId = customArcherIds[i] ?? "";
            }

            FlushArchers();
        }

        public void SetArcherSkinIds(string[] skinArcherIds)
        {
            for (int i = 0; i < (skinArcherIds?.Length ?? 0) && i < _archers.Count; i++)
            {
                _archers[i].SkinArcherId = skinArcherIds[i] ?? "";
            }

            FlushArchers();
        }

        public void SetArcherTeams(int[] teams) => _replayService.SetTeams(teams);

        private void FlushArchers() => _replayService.SetArchers(_archers);

        public bool IsRecording => _replayService.IsRecording;
        public bool IsPlayback => _replayService.IsPlayback;
        public int PlaybackFrame => _replayService.PlaybackFrame;
        public int RecordCount => _replayService.RecordCount;

        public int GetLoadedReplayMode() => _replayService.GetReplay()?.Informations?.Mode ?? -1;

        public int ConsumeNextRecordFrame() => _replayService.ConsumeNextRecord()?.Frame ?? -1;

        public int[] GetInputsAtFrame(int frame) => _replayService.GetRecordAt(frame)?.Inputs;

        public byte[] GetStateAtFrame(int frame) => _replayService.GetRecordAt(frame)?.State;

        public string StartPlayback(string replayFileName)
        {
            var result = _replayService.LoadAndStart(replayFileName).GetAwaiter().GetResult();

            if (!result.Started)
            {
                return result.FailureReason ?? "FAILED TO START REPLAY";
            }

            if ((TowerFall.Modes)(_replayService.GetReplay()?.Informations?.Mode ?? -1) == TowerFall.Modes.Trials)
            {
                TowerFall.RoundLogic.Restarted = true;
            }

            if (PlaybackStarted != null)
            {
                PlaybackStarted(result.LocalSeat);
            }
            else
            {
                //Standalone
                StateApi()?.SetDriverFlags(0, true, false, true, false, 0);
                StateApi()?.SetFrameDriver("TF.Replay");

                if (StateApi()?.IsSmoothRendering() != true)
                {
                    _fixedTimeStepBeforePlayback = TowerFall.TFGame.Instance.IsFixedTimeStep;
                    TowerFall.TFGame.Instance.IsFixedTimeStep = true;
                }

                ServiceCollections.SetInputEnabled(true);

                for (int seat = 0; seat < TowerFall.TFGame.Players.Length
                     && seat < TowerFall.TFGame.PlayerInputs.Length; seat++)
                {
                    if (TowerFall.TFGame.Players[seat])
                    {
                        TowerFall.TFGame.PlayerInputs[seat] ??= new PlaybackController(seat);
                    }
                }
            }

            _replayService.ApplyRecordedVariants();

            StandalonePlayback.BeginSession();

            _replayService.StartSession();

            return null;
        }

        public void StopPlayback()
        {
            _replayService.StopPlayback();

            PlaybackControls.Reset();

            if (StateApi()?.GetFrameDriver() == "TF.Replay")
            {
                StateApi().SetFrameDriver(null);
                StateApi().SetDriverFlags(0, false, false, false, false, 0);
                PlaybackInputs.CurrentFlat = null;

                if (_fixedTimeStepBeforePlayback != null)
                {
                    TowerFall.TFGame.Instance.IsFixedTimeStep = _fixedTimeStepBeforePlayback.Value;
                    _fixedTimeStepBeforePlayback = null;
                }
            }

            if (PlaybackStopped != null)
            {
                PlaybackStopped();
                return;
            }

            ServiceCollections.SetInputEnabled(true);
        }

        public bool SeekTo(int frame) => _replayService.SeekTo(frame);

        public bool UpdatePlaybackControls() => PlaybackControls.ShouldRunUpdate();

        public int GetTakeoverSeat() => Takeover.IsLive ? Takeover.Seat : -1;

        public bool IsTakeoverInProgress() => Takeover.SuppressesPlaybackChecks;

        public bool IsTakeoverCapturing() => Takeover.IsCapturing;

        public int[] GetTakeoverInputFlat() => Takeover.GetLiveInputFlat();

        public void SetPlaybackStartedCallback(Action<int> onStarted) => PlaybackStarted = onStarted;

        public void SetPlaybackStoppedCallback(Action onStopped) => PlaybackStopped = onStopped;

        public void SetHostCallbacks(Action<bool> setInputEnabled, Action ensureFakeControllers,Action<string> notify, Func<string, string, string> launchReplay)
            => ServiceCollections.RegisterHost(setInputEnabled, ensureFakeControllers, notify, launchReplay);
    }
}
