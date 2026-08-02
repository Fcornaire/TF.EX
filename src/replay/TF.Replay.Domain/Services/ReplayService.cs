using MessagePack;
using Microsoft.Extensions.Logging;
using TF.Replay.Domain.Extensions;
using TF.Replay.Domain.Interop;
using TF.Replay.Domain.Models;
using TF.Replay.Domain.Ports;
using TowerFall;
using static TowerFall.MatchSettings;

namespace TF.Replay.Domain.Services
{
    public class ReplayService : IReplayService
    {
        private readonly Func<IModCollections> _modCollections;
        private readonly ILogger _logger;

        private ITfStateApi StateApi() => _modCollections?.Invoke()?.ResolveState();

        private Models.Replay _replay;
        private int _currentReplayFrame;
        private int _lastFrame;
        private bool _isPlayback;
        private bool _fromFile;
        private bool? _fixedTimeStepBeforeRecording;

        private bool[] _playersBeforePlayback;
        private int[] _charactersBeforePlayback;
        private ArcherData.ArcherTypes[] _altsBeforePlayback;
        private MatchSettings _versusSettingsBeforePlayback;
        private MatchSettings _trialsSettingsBeforePlayback;
        private MatchSettings _currentSettingsBeforePlayback;
        private bool? _widerSetBeforePlayback;
        private int? _customGoalBeforePlayback;

        private static string ReplaysRoot => Path.Combine(Directory.GetCurrentDirectory(), "Replays");

        private string _month;

        public string CurrentMonth => _month ?? "";

        private string ReplaysFolder => string.IsNullOrEmpty(_month) ? ReplaysRoot : Path.Combine(ReplaysRoot, _month);

        public string CurrentFolder => ReplaysFolder;

        private static string MonthOf(DateTime moment) => moment.ToString("yyyy'-'MM");

        public string[] GetMonths()
        {
            if (!Directory.Exists(ReplaysRoot))
            {
                return [];
            }

            var folders = Directory.EnumerateDirectories(ReplaysRoot)
                .Where(dir => Directory.EnumerateFiles(dir, "*.tow").Any())
                .Select(Path.GetFileName)
                .ToList();

            var months = folders.Where(IsMonthFolder).OrderByDescending(name => name)
                .Concat(folders.Where(name => !IsMonthFolder(name)).OrderBy(name => name))
                .ToList();

            if (Directory.EnumerateFiles(ReplaysRoot, "*.tow").Any())
            {
                months.Add("");
            }

            return months.ToArray();
        }

        private static bool IsMonthFolder(string name)
            => name?.Length == 7 && name[4] == '-'
               && int.TryParse(name.AsSpan(0, 4), out _)
               && int.TryParse(name.AsSpan(5, 2), out _);

        public void SetMonth(string month) => _month = month;

        public ReplayService(Func<IModCollections> modCollections, ILogger logger)
        {
            _modCollections = modCollections;
            _logger = logger;
        }

        public string SeekBlockedBy { get; private set; }

        public bool IsRecording => _replay != null && !_isPlayback && !_fromFile;
        public bool IsPlayback => _isPlayback;
        public int PlaybackFrame => _currentReplayFrame;
        public int RecordCount => _replay?.Record.Count ?? 0;
        public int LastFrame => _lastFrame;

        public void BeginRecording(int towerId, int mode, int versusMatchLength,IEnumerable<string> variants, IEnumerable<CustomMod> mods)
        {
            if (_replay != null)
            {
                _logger?.LogDebug("A replay is already in memory ({playback}), no record", _isPlayback);
                return;
            }

            if (!RecordingPolicy.Allows(mode))
            {
                _logger?.LogDebug("Recording is off for this mode");
                return;
            }

            _lastFrame = 0;
            _fromFile = false;

            if (TowerFall.TFGame.Instance != null)
            {
                _fixedTimeStepBeforeRecording = TowerFall.TFGame.Instance.IsFixedTimeStep;
                TowerFall.TFGame.Instance.IsFixedTimeStep = true;
            }

            _replay = new Models.Replay
            {
                Informations = new ReplayInfo
                {
                    Id = towerId,
                    LocalSeat = -1,
                    Version = ReplayVersionExtensions.GetLatest(),
                    StateSchema = StateApi()?.GetStateSchemaVersion(),
                    CustomGoal = TowerFall.MatchSettings.CustomGoal,
                    Mode = mode,
                    VersusMatchLength = versusMatchLength,
                    Variants = variants?.ToList() ?? new List<string>(),
                    Mods = mods?.ToList() ?? new List<CustomMod>(),
                },
            };
        }

        public void AddRecord(byte[] state, int[] inputs, int frame = -1)
        {
            if (_replay == null)
            {
                return;
            }

            var recordedFrame = frame >= 0 ? frame : _replay.Record.Count;

            _replay.Record.Add(new Record
            {
                State = state,
                Inputs = inputs,
                Frame = recordedFrame,
            });

            _lastFrame = Math.Max(_lastFrame, recordedFrame);
        }

        public void RemovePredictedRecords(int frame)
        {
            if (_replay == null)
            {
                return;
            }

            _replay.Record.RemoveAll(rec => rec.Frame > frame);
            _lastFrame = Math.Min(_lastFrame, frame);
        }

        public void AddDesyncRecord(byte[] state, int[] inputs, int frame)
        {
            _replay?.Desynchs.Add(new Record { State = state, Inputs = inputs, Frame = frame });
        }

        public string Export()
        {
            if (_replay == null || _fromFile || !_replay.Record.Any())
            {
                return null;
            }

            _replay.Informations.MatchLength = TimeSpan.FromSeconds(_replay.Record.Count / 60);

            var folder = Path.Combine(ReplaysRoot, MonthOf(DateTime.UtcNow));

            Directory.CreateDirectory(folder);

            var filePath = NextFreeReplayPath(folder, out var filename);
            _replay.Informations.Name = filename;

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                MessagePackSerializer.Serialize(fileStream, _replay, ReplaySerialization.Default);
            }

            _logger?.LogDebug("Exported replay {name} ({count} frames)", filename, _replay.Record.Count);

            Reset();

            return filePath;
        }

        public void Reset()
        {
            _replay = null;
            _currentReplayFrame = 0;
            _lastFrame = 0;
            _isPlayback = false;
            _fromFile = false;

            if (_fixedTimeStepBeforeRecording != null && TowerFall.TFGame.Instance != null)
            {
                TowerFall.TFGame.Instance.IsFixedTimeStep = _fixedTimeStepBeforeRecording.Value;
                _fixedTimeStepBeforeRecording = null;
            }
        }

        private static string NextFreeReplayPath(string folder, out string filename)
        {
            var stamp = DateTime.UtcNow.ToString("dd'-'MM'-'yyy'T'HH'-'mm'-'ss");

            for (int attempt = 0; ; attempt++)
            {
                filename = attempt == 0 ? $"{stamp}.tow" : $"{stamp}-{attempt}.tow";

                var candidate = Path.Combine(folder, filename);

                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        public void SetLocalSeat(int seat)
        {
            if (_replay != null) _replay.Informations.LocalSeat = seat;
        }

        public void SetPlayerCount(int count)
        {
            if (_replay != null) _replay.Informations.PlayerCount = count;
        }

        public void SetTeams(int[] teams)
        {
            if (_replay != null)
            {
                _replay.Informations.Teams = teams ?? [];
            }
        }

        public void SetSeed(int seed)
        {
            if (_replay != null) _replay.Informations.Seed = seed;
        }

        public void SetTrialsTime(long ticks)
        {
            if (_replay != null) _replay.Informations.TrialsTimeTicks = ticks;
        }

        public void SetTrialsLevel(int x, int y)
        {
            if (_replay == null)
            {
                return;
            }

            _replay.Informations.Id = x;
            _replay.Informations.TrialsLevelY = y;
        }

        public void SetArchers(IEnumerable<ArcherInfo> archers)
        {
            if (_replay != null) _replay.Informations.Archers = archers?.ToList() ?? new List<ArcherInfo>();
        }

        public bool NeedsMigration(ReplayInfo informations)
        {
            var current = CurrentStateSchema();

            return informations != null
                && !string.IsNullOrEmpty(current)
                && informations.StateSchemaOrLegacy != current;
        }

        private string CurrentStateSchema() => StateApi()?.GetStateSchemaVersion();

        public async Task<PlaybackStartResult> LoadAndStart(string replayFilename)
        {
            try
            {
                var filePath = Path.Combine(ReplaysFolder, replayFilename);

                Models.Replay replay;

                try
                {
                    replay = await ToReplay(filePath);
                }
                catch (Exception e)
                {
                    _logger?.LogDebug("{file} could not be read: {message}", replayFilename, e.Message);
                    TryRenameObsolete(filePath);
                    return PlaybackStartResult.Fail("REPLAY VERSION IS OBSOLETE");
                }

                if (replay == null || !replay.Record.Any())
                {
                    return PlaybackStartResult.Fail("REPLAY IS EMPTY");
                }

                if (NeedsMigration(replay.Informations))
                {
                    _logger?.LogDebug("{file} was recorded on state schema {schema}, current is {current}",replayFilename, replay.Informations.StateSchemaOrLegacy, CurrentStateSchema());

                    return PlaybackStartResult.Fail("REPLAY NEEDS MIGRATION");
                }

                var missingMod = MissingMod(replay.Informations);

                if (missingMod != null)
                {
                    _logger?.LogWarning("{file} needs uninstalled {mod}", replayFilename, missingMod);

                    return PlaybackStartResult.Fail($"{missingMod} IS MISSING");
                }

                WarnOnModVersions(replay.Informations);

                SeekBlockedBy = ModWithoutStateEvents(replay.Informations);

                if (SeekBlockedBy != null)
                {
                    _logger?.LogInformation("{file} carries state from {mod}, which registered no save/load events: seeking is off",replayFilename, SeekBlockedBy);
                }

                LoadReplay(replay);

                SnapshotSelection();

                ApplyMods(replay.Informations);

                var archers = replay.Informations.Archers.ToArray();
                var usedArchers = archers.Select(archer => archer.Index);

                for (int seat = 0; seat < archers.Length; seat++)
                {
                    var (archerIndex, altIndex) = ArcherDataExtensions.EnsureArcherDataExist(
                        archers[seat].Index, (int)archers[seat].Type, usedArchers);

                    TFGame.Characters[seat] = archerIndex;
                    TFGame.AltSelect[seat] = (ArcherData.ArcherTypes)altIndex;
                }

                var playerCount = replay.Informations.PlayerCount > 0
                    ? replay.Informations.PlayerCount
                    : archers.Length;

                for (int seat = 0; seat < TFGame.Players.Length; seat++)
                {
                    TFGame.Players[seat] = seat < playerCount;
                }

                StateApi()?.SetSeed(replay.Informations.Seed);

                var matchSettings = BuildMatchSettings(replay.Informations);

                ApplyTeams(matchSettings, replay.Informations);

                var unknownVariants = matchSettings.Variants.ApplyVariants(replay.Informations.Variants);

                if (unknownVariants.Count > 0)
                {
                    var names = string.Join(", ", unknownVariants);

                    _logger?.LogWarning("{file} uses variants this install does not have: {variants}", replayFilename, names);
                    ServiceCollections.Notify($"MISSING VARIANTS: {names}".ToUpperInvariant());
                }

                matchSettings.MatchLength = (MatchLengths)replay.Informations.VersusMatchLength;

                if (replay.Informations.CustomGoal > 0)
                {
                    _customGoalBeforePlayback = TowerFall.MatchSettings.CustomGoal;
                    TowerFall.MatchSettings.CustomGoal = replay.Informations.CustomGoal;
                }

                matchSettings.RandomLevelSeed = replay.Informations.Seed;

                if ((Modes)replay.Informations.Mode == Modes.Trials)
                {
                    TowerFall.MainMenu.TrialsMatchSettings = matchSettings;
                }
                else
                {
                    TowerFall.MainMenu.VersusMatchSettings = matchSettings;
                }

                TowerFall.MainMenu.CurrentMatchSettings = matchSettings;

                _currentReplayFrame = 0;
                _isPlayback = true;

                return PlaybackStartResult.Ok(replay.Informations.LocalSeat);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while loading replay {name}", replayFilename);
                _isPlayback = false;
                return PlaybackStartResult.Fail("FAILED TO LOAD REPLAY");
            }
        }

        private static MatchSettings BuildMatchSettings(ReplayInfo informations)
        {
            if ((Modes)informations.Mode == Modes.Trials)
            {
                return new MatchSettings(
                    new TrialsLevelSystem(GameData.TrialsLevels[informations.Id, informations.TrialsLevelY]),
                    Modes.Trials,
                    MatchLengths.Standard);
            }

            return new MatchSettings(
                GameData.VersusTowers[informations.Id].GetLevelSystem(),
                (Modes)informations.Mode,
                MatchLengths.Standard);
        }

        private void ApplyTeams(MatchSettings matchSettings, ReplayInfo informations)
        {
            if (!matchSettings.TeamMode)
            {
                return;
            }

            var recorded = informations.Teams ?? [];
            var known = recorded.Any(team => team >= 0);

            for (int seat = 0; seat < 4; seat++)
            {
                matchSettings.Teams[seat] = known
                    ? (Allegiance)(seat < recorded.Length ? recorded[seat] : (int)Allegiance.Neutral)
                    : seat % 2 == 0 ? Allegiance.Blue : Allegiance.Red;
            }

            if (!known)
            {
                _logger?.LogDebug(
                    "{name} predates team recording; guessing an even/odd split, playback may diverge",
                    informations.Name);
            }
        }

        public void StartSession()
        {
            var settings = (Modes?)_replay?.Informations?.Mode == Modes.Trials
                ? TowerFall.MainMenu.TrialsMatchSettings
                : TowerFall.MainMenu.VersusMatchSettings;

            new Session(settings).StartGame();
        }

        public void StopPlayback()
        {
            _isPlayback = false;
            _currentReplayFrame = 0;

            _replay = null;
            _fromFile = false;
            _lastFrame = 0;
            SeekBlockedBy = null;

            RestoreSelection();
        }

        public string MissingMod(ReplayInfo informations)
        {
            var catalog = _modCollections?.Invoke();

            if (catalog == null || informations?.Mods == null)
            {
                return null;
            }

            return informations.Mods
                .Where(mod => !string.IsNullOrEmpty(mod?.Name))
                .FirstOrDefault(mod => catalog.GetVersion(mod.Name) == null)
                ?.Name;
        }

        private static string ModWithoutStateEvents(ReplayInfo informations)
        {
            return informations?.Mods?.FirstOrDefault
                (mod => mod?.Data != null
                    && mod.Data.TryGetValue(ModData.StateEventsKey, out var registered)
                    && registered == false.ToString())
                ?.Name;
        }

        private void WarnOnModVersions(ReplayInfo informations)
        {
            var catalog = _modCollections?.Invoke();

            if (catalog == null || informations?.Mods == null)
            {
                return;
            }

            foreach (var mod in informations.Mods)
            {
                if (string.IsNullOrEmpty(mod?.Name)
                    || !mod.Data.TryGetValue(ModData.VersionKey, out var recorded)
                    || string.IsNullOrEmpty(recorded))
                {
                    continue;
                }

                var installed = catalog.GetVersion(mod.Name);

                if (installed == null || installed == recorded)
                {
                    continue;
                }

                _logger?.LogWarning("Replay was recorded on {mod} {recorded}, this install has {installed}",mod.Name, recorded, installed);

                ServiceCollections.Notify($"{mod.Name} IS {installed}, REPLAY USED {recorded}".ToUpperInvariant());
            }
        }

        private void ApplyMods(ReplayInfo informations)
        {
            var widerSet = _modCollections?.Invoke()?.ResolveWiderSet();

            if (widerSet == null)
            {
                return;
            }

            _widerSetBeforePlayback = widerSet.IsWide;

            var recorded = informations?.Mods?.FirstOrDefault(mod => string.Equals(mod?.Name, ModData.WiderSetName, StringComparison.OrdinalIgnoreCase));

            ApplyWide(widerSet, recorded != null
                && recorded.Data.TryGetValue(ModData.IsWideKey, out var value)
                && bool.TryParse(value, out var wide)
                && wide);
        }

        private static void ApplyWide(IWiderSetModApi widerSet, bool wide)
        {
            widerSet.IsWide = wide;

            var screen = Monocle.Engine.Instance?.Screen;

            if (screen != null)
            {
                screen.PadOffset = wide ? -50f : 0f;
            }
        }

        private void SnapshotSelection()
        {
            _playersBeforePlayback = (bool[])TFGame.Players.Clone();
            _charactersBeforePlayback = (int[])TFGame.Characters.Clone();
            _altsBeforePlayback = (ArcherData.ArcherTypes[])TFGame.AltSelect.Clone();
            _versusSettingsBeforePlayback = TowerFall.MainMenu.VersusMatchSettings;
            _trialsSettingsBeforePlayback = TowerFall.MainMenu.TrialsMatchSettings;
            _currentSettingsBeforePlayback = TowerFall.MainMenu.CurrentMatchSettings;
        }

        private void RestoreSelection()
        {
            if (_customGoalBeforePlayback != null)
            {
                TowerFall.MatchSettings.CustomGoal = _customGoalBeforePlayback.Value;
                _customGoalBeforePlayback = null;
            }

            if (_widerSetBeforePlayback != null)
            {
                var widerSet = _modCollections?.Invoke()?.ResolveWiderSet();

                if (widerSet != null)
                {
                    ApplyWide(widerSet, _widerSetBeforePlayback.Value);
                }

                _widerSetBeforePlayback = null;
            }

            if (_playersBeforePlayback == null)
            {
                return;
            }

            Array.Copy(_playersBeforePlayback, TFGame.Players, _playersBeforePlayback.Length);
            Array.Copy(_charactersBeforePlayback, TFGame.Characters, _charactersBeforePlayback.Length);
            Array.Copy(_altsBeforePlayback, TFGame.AltSelect, _altsBeforePlayback.Length);

            TowerFall.MainMenu.VersusMatchSettings = _versusSettingsBeforePlayback;
            TowerFall.MainMenu.TrialsMatchSettings = _trialsSettingsBeforePlayback;
            TowerFall.MainMenu.CurrentMatchSettings = _currentSettingsBeforePlayback;

            _playersBeforePlayback = null;
            _charactersBeforePlayback = null;
            _altsBeforePlayback = null;
            _versusSettingsBeforePlayback = null;
            _trialsSettingsBeforePlayback = null;
            _currentSettingsBeforePlayback = null;
        }

        public Record GetRecordAt(int frame)
        {
            return _replay?.Record.Find(rec => rec.Frame == frame);
        }

        public Record ConsumeNextRecord()
        {
            if (_replay == null)
            {
                return null;
            }

            var record = _replay.Record.Find(rec => rec.Frame == _currentReplayFrame);

            _currentReplayFrame++;

            return record;
        }

        public bool SeekTo(int frame)
        {
            if (_replay == null || !_replay.Record.Any())
            {
                return false;
            }

            var target = SkipRoundIntro(Math.Clamp(frame, 0, _lastFrame));

            if (!RestoreStateAt(target))
            {
                return false;
            }

            _currentReplayFrame = target;
            return true;
        }

        private bool IsTrials => (Modes?)_replay?.Informations?.Mode == Modes.Trials;

        private int SkipRoundIntro(int target)
        {
            const int Coarse = 60;
            const int Fine = 5;
            const int MaxScan = 3600;

            var api = StateApi();

            if (api == null || IsTrials || RoundHasStarted(api, target))
            {
                return target;
            }

            var previous = target;

            for (int scanned = Coarse; scanned <= MaxScan; scanned += Coarse)
            {
                var candidate = Math.Min(target + scanned, _lastFrame);

                if (RoundHasStarted(api,candidate))
                {
                    for (int fine = previous + Fine; fine < candidate; fine += Fine)
                    {
                        if (RoundHasStarted(api,fine))
                        {
                            return fine;
                        }
                    }

                    return candidate;
                }

                if (candidate >= _lastFrame)
                {
                    return target;
                }

                previous = candidate;
            }

            _logger?.LogWarning("Could not find the end of the round intro within {frames} frames", MaxScan);

            return target;
        }

        private bool RoundHasStarted(ITfStateApi api, int frame)
        {
            var record = _replay.Record.Find(rec => rec.Frame == frame);

            return record?.State == null || api.IsRoundStarted(record.State);
        }

        public bool RestoreStateAt(int frame)
        {
            var record = _replay?.Record.Find(rec => rec.Frame == frame);

            return record?.State != null
                && StateApi() is not null
                && StateApi().LoadGameStateBytes(record.State);
        }

        public Models.Replay GetReplay() => _replay;

        public void LoadReplay(Models.Replay replay)
        {
            _replay = replay;
            _fromFile = true;
            _lastFrame = replay != null && replay.Record.Any() ? replay.Record.Max(rec => rec.Frame) : 0;
        }

        public async Task<IEnumerable<Models.Replay>> LoadAndGetReplays()
        {
            if (!Directory.Exists(ReplaysFolder))
            {
                return Enumerable.Empty<Models.Replay>();
            }

            var files = Directory.EnumerateFiles(ReplaysFolder, "*.tow").ToList();
            var replays = new List<Models.Replay>();

            await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (file, _) =>
                {
                    Models.Replay replay;

                    try
                    {
                        replay = await ToReplay(file, headerOnly: true);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogDebug("{file} could not be read ({message}), treating as obsolete",Path.GetFileName(file), e.Message);
                        TryRenameObsolete(file);
                        return;
                    }

                    if (replay?.Informations == null)
                    {
                        TryRenameObsolete(file);
                        return;
                    }

                    replay.Informations.Name ??= Path.GetFileName(file);

                    lock (replays)
                    {
                        replays.Add(replay);
                    }
                });

            return replays.OrderBy(replay => replay.Informations.Name).ToList();
        }

        private void TryRenameObsolete(string file)
        {
            try
            {
                File.Move(file, file + ".obsolete", overwrite: true);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Could not rename obsolete replay {file}", file);
            }
        }

        private async Task<Models.Replay> ToReplay(string filePath, bool headerOnly = false)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var options = headerOnly ? ReplaySerialization.WithoutRecords : ReplaySerialization.Default;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            return await MessagePackSerializer.DeserializeAsync<Models.Replay>(stream, options);
        }
    }
}
