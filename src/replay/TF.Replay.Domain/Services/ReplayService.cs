using MessagePack;
using Microsoft.Extensions.Logging;
using System.Globalization;
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
        private const int SeatScanLimit = 120;
        private const int KeyframesPerSecond = 16;
        private const int StateSnapWindow = 64;
        private bool? _fixedTimeStepBeforeRecording;
        private bool _tickRateOverridden;

        private bool[] _playersBeforePlayback;
        private int[] _charactersBeforePlayback;
        private ArcherData.ArcherTypes[] _altsBeforePlayback;
        private MatchSettings _versusSettingsBeforePlayback;
        private MatchSettings _trialsSettingsBeforePlayback;
        private MatchSettings _currentSettingsBeforePlayback;
        private bool? _widerSetBeforePlayback;
        private int? _customGoalBeforePlayback;

        private static string SavesRootFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", "TF.Replay");

        internal static string ReplaysRootFolder => Path.Combine(SavesRootFolder, "Replays");

        internal static string GifsRootFolder => Path.Combine(SavesRootFolder, "Gifs");

        internal static string GetLegacyReplaysFolder => Path.Combine(Directory.GetCurrentDirectory(), "Replays");

        private string _month;

        public string CurrentMonth => _month ?? "";

        private string ReplaysFolder => string.IsNullOrEmpty(_month) ? ReplaysRootFolder : Path.Combine(ReplaysRootFolder, _month);

        public string CurrentFolder => ReplaysFolder;

        private static string MonthOf(DateTime moment) => moment.ToString("yyyy'-'MM");

        public string[] GetMonths()
        {
            if (!Directory.Exists(ReplaysRootFolder))
            {
                return [];
            }

            var folders = Directory.EnumerateDirectories(ReplaysRootFolder)
                .Where(dir => Directory.EnumerateFiles(dir, "*.tow").Any())
                .Select(Path.GetFileName)
                .ToList();

            var months = folders.Where(IsMonthFolder).OrderByDescending(name => name)
                .Concat(folders.Where(name => !IsMonthFolder(name)).OrderBy(name => name))
                .ToList();

            if (Directory.EnumerateFiles(ReplaysRootFolder, "*.tow").Any())
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

        public void BeginRecording(int towerId, int mode, int versusMatchLength, IEnumerable<string> variants, IEnumerable<CustomMod> mods)
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
                _fixedTimeStepBeforeRecording ??= TowerFall.TFGame.Instance.IsFixedTimeStep;
                TowerFall.TFGame.Instance.IsFixedTimeStep = true;
            }

            _replay = new Models.Replay
            {
                Informations = new ReplayInfo
                {
                    Id = towerId,
                    LocalSeat = -1,
                    Version = ReplayVersionExtensions.GetLatest(),
                    TickRate = CurrentTickRate(),
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
                State = recordedFrame % KeyframeStride() == 0 ? state : null,
                Inputs = inputs,
                Frame = recordedFrame,
            });

            _lastFrame = Math.Max(_lastFrame, recordedFrame);
        }

        private int KeyframeStride() => RecordingPolicy.FullStates ? 1 : Math.Max(1, CurrentTickRate() / KeyframesPerSecond);

        public void RemovePredictedRecords(int frame)
        {
            if (_replay == null)
            {
                return;
            }

            _replay.Record.RemoveAll(rec => rec.Frame > frame);
            _lastFrame = Math.Min(_lastFrame, frame);
        }

        public string Export()
        {
            if (_replay == null || _fromFile || !_replay.Record.Any())
            {
                if (_replay != null && !_fromFile)
                {
                    Reset();
                }

                return null;
            }

            if (StateApi()?.IsTestMode() == true)
            {
                _logger?.LogDebug("Test mode, ignore record");
                Reset();

                return null;
            }

            //some variants cannot be reproduced by a replay at all
            var banned = GetBannedVariant(_replay.Informations);

            if (banned != null)
            {
                _logger?.LogInformation("Not saving the replay: {variant} cannot be replayed", banned);
                ServiceCollections.Notify($"NO REPLAY: {banned} CANNOT BE REPLAYED".ToUpperInvariant());
                Reset();

                return null;
            }

            _replay.Informations.MatchLength = TimeSpan.FromSeconds(_replay.Record.Count / (double)_replay.Informations.TickRateOrLegacy);

            var folder = Path.Combine(ReplaysRootFolder, MonthOf(DateTime.UtcNow));

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
            RestoreTickRateOverride();

            _replay = null;
            _currentReplayFrame = 0;
            _lastFrame = 0;
            _isPlayback = false;
            _fromFile = false;

            RestorePendingTimeStep();
        }

        public void RestorePendingTimeStep()
        {
            if (_fixedTimeStepBeforeRecording != null && TowerFall.TFGame.Instance != null && TowerFall.TFGame.Instance.Scene is not TowerFall.Level)
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
                    _logger?.LogDebug("{file} was recorded on state schema {schema}, current is {current}", replayFilename, replay.Informations.StateSchemaOrLegacy, CurrentStateSchema());

                    return PlaybackStartResult.Fail("REPLAY NEEDS MIGRATION");
                }

                var missingMod = MissingMod(replay.Informations);

                if (missingMod != null)
                {
                    _logger?.LogWarning("{file} needs uninstalled {mod}", replayFilename, missingMod);

                    return PlaybackStartResult.Fail($"{missingMod} IS MISSING");
                }

                var missingVariants = GetMissingVariants(replay.Informations);

                if (missingVariants.Count > 0)
                {
                    _logger?.LogWarning("{file} uses variants this install does not have: {variants}", replayFilename, string.Join(", ", missingVariants));

                    return PlaybackStartResult.Fail(GetMissingVariantsFailureMessage(missingVariants));
                }

                WarnOnModVersions(replay.Informations);

                SeekBlockedBy = ModWithoutStateEvents(replay.Informations);

                if (SeekBlockedBy != null)
                {
                    _logger?.LogInformation("{file} uses {source}, which registered no state events: seeking is off", replayFilename, SeekBlockedBy);
                }

                LoadReplay(replay);

                SnapshotSelection();

                ApplyMods(replay.Informations);

                ApplyRecordedArchers();

                StateApi()?.SetSeed(replay.Informations.Seed);

                var matchSettings = BuildMatchSettings(replay.Informations);

                ApplyTeams(matchSettings, replay.Informations);

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

        private void ApplyRecordedArchers()
        {
            var archers = _replay?.Informations?.Archers?.ToArray();

            if (archers == null || archers.Length == 0)
            {
                return;
            }

            var seats = SeatsFor(archers);

            var wantedIndexes = archers.Select(archer => string.IsNullOrEmpty(archer.CustomArcherId)
                    ? archer.Index
                    : ArcherDataExtensions.ResolveCustomArcher(archer.CustomArcherId))
                .ToArray();

            var taken = new HashSet<int>();

            for (int i = 0; i < archers.Length; i++)
            {
                if (ArcherDataExtensions.Exists(wantedIndexes[i], (int)archers[i].Type))
                {
                    taken.Add(wantedIndexes[i]);
                }
            }

            for (int seat = 0; seat < TFGame.Players.Length; seat++)
            {
                TFGame.Players[seat] = false;
            }

            var skinSeats = new List<int>();
            var skinIds = new List<string>();

            for (int i = 0; i < archers.Length; i++)
            {
                var seat = seats[i];

                if (seat < 0 || seat >= TFGame.Characters.Length)
                {
                    continue;
                }

                var (archerIndex, altIndex) = ArcherDataExtensions.EnsureArcherDataExist(wantedIndexes[i], (int)archers[i].Type, taken);

                TFGame.Characters[seat] = archerIndex;
                TFGame.AltSelect[seat] = (ArcherData.ArcherTypes)altIndex;
                TFGame.Players[seat] = true;

                var skinId = string.IsNullOrEmpty(archers[i].SkinArcherId) ? archers[i].CustomArcherId : archers[i].SkinArcherId;

                if (!string.IsNullOrEmpty(skinId))
                {
                    skinSeats.Add(seat);
                    skinIds.Add(skinId);
                }
            }

            SetReplaySkinSeats(skinSeats.ToArray(), skinIds.ToArray());
        }

        private void SetReplaySkinSeats(int[] seats, string[] skinIds)
        {
            try
            {
                var api = _modCollections?.Invoke()?.ResolveTfExArcherSkin();

                if (seats.Length > 0)
                {
                    api?.SetReplaySkinSeats(seats, skinIds);
                }
                else
                {
                    api?.ClearReplaySkinSeats();
                }
            }
            catch (Exception e)
            {
                _logger?.LogDebug("Could not hand the replay skins to EX: {error}", e.Message);
            }
        }

        private void ClearReplaySkinSeats()
        {
            try
            {
                _modCollections?.Invoke()?.ResolveTfExArcherSkin()?.ClearReplaySkinSeats();
            }
            catch (Exception)
            {
            }
        }

        private int[] SeatsFor(Models.ArcherInfo[] archers)
        {
            if (archers.All(archer => archer.Seat.HasValue))
            {
                return archers.Select(archer => archer.Seat.Value).ToArray();
            }

            var recorded = RecordedSeats();

            if (recorded != null && recorded.Length == archers.Length)
            {
                _logger?.LogDebug("[Replay] Archer seats are not stored", string.Join(",", recorded));

                return recorded;
            }

            return Enumerable.Range(0, archers.Length).ToArray();
        }

        private int[] RecordedSeats()
        {
            var state = StateApi();

            if (state == null || _replay?.Record == null)
            {
                return null;
            }

            var scanned = 0;

            foreach (var record in _replay.Record)
            {
                if (record.State == null || scanned++ > SeatScanLimit)
                {
                    continue;
                }

                string[] described;

                try
                {
                    described = state.DescribePlayers(record.State);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, "[Replay] Could not read the seats from a recorded state");
                    return null;
                }

                if (described == null || described.Length == 0)
                {
                    continue;
                }

                var seats = new List<int>();

                foreach (var line in described)
                {
                    if (int.TryParse(line.Split(';')[0], out var seat))
                    {
                        seats.Add(seat);
                    }
                }

                return seats.Count == described.Length ? seats.OrderBy(seat => seat).ToArray() : null;
            }

            return null;
        }

        public void StartSession()
        {
            var settings = (Modes?)_replay?.Informations?.Mode == Modes.Trials
                ? TowerFall.MainMenu.TrialsMatchSettings
                : TowerFall.MainMenu.VersusMatchSettings;

            new Session(settings).StartGame();
        }

        public void ApplyRecordedVariants()
        {
            var informations = _replay?.Informations;

            if (informations == null)
            {
                return;
            }

            TowerFall.MainMenu.CurrentMatchSettings?.Variants.ApplyVariants(informations.Variants);
        }

        public void StopPlayback()
        {
            RestoreTickRateOverride();

            _isPlayback = false;
            _currentReplayFrame = 0;

            _replay = null;
            _fromFile = false;
            _lastFrame = 0;
            SeekBlockedBy = null;

            RemovePlaybackControllers();

            RestoreSelection();
        }

        private static void RemovePlaybackControllers()
        {
            for (int seat = 0; seat < TFGame.PlayerInputs.Length; seat++)
            {
                if (TFGame.PlayerInputs[seat] is PlaybackController)
                {
                    TFGame.PlayerInputs[seat] = null;
                }
            }

            for (int i = 0; i < MenuInput.MenuInputs.Length; i++)
            {
                if (MenuInput.MenuInputs[i] is PlaybackController)
                {
                    MenuInput.MenuInputs[i] = null;
                }
            }
        }

        public void EnsurePlaybackTickRate()
        {
            if (!_isPlayback || _replay == null)
            {
                return;
            }

            var game = TowerFall.TFGame.Instance;

            if (game == null || !game.IsFixedTimeStep)
            {
                return;
            }

            var rate = _replay.Informations?.TickRateOrLegacy ?? ReplayInfo.LegacyTickRate;

            if (CurrentTickRate() == rate)
            {
                return;
            }

            game.TargetElapsedTime = TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerSecond / (double)rate));
            _tickRateOverridden = true;
            _logger?.LogDebug("Playback engine tick rate set to {rate}", rate);
        }

        private void RestoreTickRateOverride()
        {
            if (!_tickRateOverridden)
            {
                return;
            }

            _tickRateOverridden = false;

            if (TowerFall.TFGame.Instance?.IsFixedTimeStep == true)
            {
                TowerFall.TFGame.Instance.IsFixedTimeStep = true;
            }
        }

        private static int CurrentTickRate()
        {
            var game = TowerFall.TFGame.Instance;

            if (game == null || !game.IsFixedTimeStep)
            {
                return ReplayInfo.LegacyTickRate;
            }

            return (int)Math.Round(1.0 / game.TargetElapsedTime.TotalSeconds);
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

        private static string GetBannedVariant(ReplayInfo informations)
        {
            return informations?.Mods?
                .Where(mod => mod?.Data != null)
                .Select(mod => mod.Data.TryGetValue(ModData.BannedVariantsKey, out var banned) ? banned : null)
                .FirstOrDefault(banned => !string.IsNullOrEmpty(banned));
        }

        public List<string> GetMissingVariants(ReplayInfo informations)
        {
            var set = TowerFall.MainMenu.VersusMatchSettings?.Variants;

            if (set == null || informations?.Variants == null)
            {
                return new List<string>();
            }

            return informations.Variants
                .Where(name => set.Variants.All(variant => variant.Title != name) && set.CustomVariants.All(custom => custom.Value?.Title != name))
                .ToList();
        }

        private static string GetMissingVariantsFailureMessage(ICollection<string> names)
        {
            const float MaxWidth = 290f;
            const string BrowserPrefix = "CANNOT RESTART REPLAY: ";

            var shown = new List<string>();

            foreach (var name in names)
            {
                var candidate = new List<string>(shown) { name };

                if (shown.Count > 0 && TowerFall.TFGame.Font.MeasureString(BrowserPrefix + MissingVariantsFailureMessage(candidate, names.Count)).X > MaxWidth)
                {
                    break;
                }

                shown.Add(name);
            }

            return MissingVariantsFailureMessage(shown, names.Count);
        }

        private static string MissingVariantsFailureMessage(ICollection<string> shown, int total)
        {
            var hidden = total - shown.Count;
            var suffix = hidden > 0 ? $" +{hidden} MORE" : "";

            return $"MISSING VARIANTS: {string.Join(", ", shown)}{suffix}";
        }

        private static string ModWithoutStateEvents(ReplayInfo informations)
        {
            var mod = informations?.Mods?.FirstOrDefault
                (mod => mod?.Data != null
                    && mod.Data.TryGetValue(ModData.StateEventsKey, out var registered)
                    && registered == false.ToString());

            if (mod == null)
            {
                return null;
            }

            return mod.Data.TryGetValue(ModData.UnstatedVariantsKey, out var variants) && !string.IsNullOrEmpty(variants) ? variants : mod.Name;
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

                _logger?.LogWarning("Replay was recorded on {mod} {recorded}, this install has {installed}", mod.Name, recorded, installed);

                var shortName = mod.Name.LastIndexOf('.') is int separator && separator > 0 && separator < mod.Name.Length - 1
                    ? mod.Name.Substring(separator + 1)
                    : mod.Name;

                ServiceCollections.Notify($"{shortName} {installed} (REPLAY WAS {recorded}) - MAY NOT WORK".ToUpperInvariant());
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

            ClearReplaySkinSeats();

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
            var landed = RestoreNearestStateAt(target);

            if (landed < 0)
            {
                return false;
            }

            _currentReplayFrame = landed;
            return true;
        }

        public int PreviousStateFrame(int frame)
        {
            var floor = Math.Max(0, frame - 1 - StateSnapWindow);

            for (int f = frame - 1; f >= floor; f--)
            {
                if (GetRecordAt(f)?.State != null)
                {
                    return f;
                }
            }

            return Math.Max(0, frame - 1);
        }

        private Record NearestStateRecord(int frame)
        {
            for (int distance = 0; distance <= StateSnapWindow; distance++)
            {
                var record = GetRecordAt(frame - distance);

                if (record?.State != null)
                {
                    return record;
                }

                if (distance > 0)
                {
                    record = GetRecordAt(frame + distance);

                    if (record?.State != null)
                    {
                        return record;
                    }
                }
            }

            return null;
        }

        private bool IsTrials => (Modes?)_replay?.Informations?.Mode == Modes.Trials;

        private int SkipRoundIntro(int target)
        {
            var tickScale = Math.Max(1, (_replay?.Informations?.TickRateOrLegacy ?? ReplayInfo.LegacyTickRate) / 60);
            int Coarse = 60 * tickScale;
            int Fine = 5 * tickScale;
            int MaxScan = 3600 * tickScale;

            var api = StateApi();

            if (api == null || IsTrials || RoundHasStarted(api, target))
            {
                return target;
            }

            var previous = target;

            for (int scanned = Coarse; scanned <= MaxScan; scanned += Coarse)
            {
                var candidate = Math.Min(target + scanned, _lastFrame);

                if (RoundHasStarted(api, candidate))
                {
                    for (int fine = previous + Fine; fine < candidate; fine += Fine)
                    {
                        if (RoundHasStarted(api, fine))
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
            var record = NearestStateRecord(frame);

            return record == null || api.IsRoundStarted(record.State);
        }

        public int SeekLandingFor(int frame)
        {
            if (_replay == null || !_replay.Record.Any())
            {
                return -1;
            }

            var target = SkipRoundIntro(Math.Clamp(frame, 0, _lastFrame));

            return NearestStateRecord(target)?.Frame ?? -1;
        }

        public bool RestoreStateAt(int frame) => RestoreNearestStateAt(frame) >= 0;

        private int RestoreNearestStateAt(int frame)
        {
            var record = NearestStateRecord(frame);

            return record != null && StateApi() is not null && StateApi().LoadGameStateBytes(record.State)
                ? record.Frame
                : -1;
        }

        public Models.Replay GetReplay() => _replay;

        public void LoadReplay(Models.Replay replay)
        {
            _replay = replay;
            _fromFile = true;
            _lastFrame = replay != null && replay.Record.Any() ? replay.Record.Max(rec => rec.Frame) : 0;
        }

        public async Task<IEnumerable<Models.Replay>> LoadAndGetReplays(Action<int, int> onProgress = null)
        {
            if (!Directory.Exists(ReplaysFolder))
            {
                return Enumerable.Empty<Models.Replay>();
            }

            var files = Directory.EnumerateFiles(ReplaysFolder, "*.tow").ToList();
            var slots = new Models.Replay[files.Count];
            var processed = 0;

            await Parallel.ForEachAsync(Enumerable.Range(0, files.Count), new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (index, _) =>
                {
                    var file = files[index];

                    try
                    {
                        Models.Replay replay;

                        try
                        {
                            replay = await ToReplay(file, headerOnly: true);
                        }
                        catch (Exception e)
                        {
                            _logger?.LogDebug("{file} could not be read ({message}), treating as obsolete", Path.GetFileName(file), e.Message);
                            TryRenameObsolete(file);
                            return;
                        }

                        if (replay?.Informations == null)
                        {
                            TryRenameObsolete(file);
                            return;
                        }

                        replay.Informations.Name ??= Path.GetFileName(file);
                        slots[index] = replay;
                    }
                    finally
                    {
                        onProgress?.Invoke(Interlocked.Increment(ref processed), files.Count);
                    }
                });

            return slots.Where(replay => replay != null)
                .OrderByDescending(replay => RecordedAt(replay.Informations.Name))
                .ThenByDescending(replay => replay.Informations.Name)
                .ToList();
        }

        private static DateTime RecordedAt(string name)
        {
            return DateTime.TryParseExact(Path.GetFileNameWithoutExtension(name ?? string.Empty),
                "dd-MM-yyyy'T'HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                ? at
                : DateTime.MinValue;
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
