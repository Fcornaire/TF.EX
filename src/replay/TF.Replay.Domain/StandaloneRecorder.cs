using Microsoft.Extensions.Logging;
using MonoMod.Utils;
using TF.Replay.Domain.Extensions;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class StandaloneRecorder
    {
        private static readonly Modes[] Recordable =
        {
            Modes.LastManStanding,
            Modes.HeadHunters,
            Modes.TeamDeathmatch,
            Modes.Trials,
        };

        private static bool _consuming;
        private static bool _isTrials;
        private static bool _halted;
        private static bool _paused;
        private static int _frame;
        private static bool _warm;

        private static int[] _seats;
        private static int[] _characters;
        private static int[] _alts;
        private static int[] _teams;
        private static int[] _scores;
        private static int _winner = -1;

        public static bool IsActive { get; private set; }

        public static bool ExportPending { get; private set; }

        public static void TryBegin(Session session)
        {
            if (ExportPending)
            {
                ServiceCollections.ResolveApi().ResetRecording();
            }

            Reset();

            var api = ServiceCollections.ResolveApi();
            var service = ServiceCollections.ResolveReplayService();
            var state = ServiceCollections.ResolveStateApi();
            var settings = session?.MatchSettings;

            if (api == null || service == null || state == null || settings == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(state.GetFrameDriver()) || service.IsPlayback)
            {
                return;
            }

            if (Array.IndexOf(Recordable, settings.Mode) < 0)
            {
                return;
            }

            var seed = Environment.TickCount;

            state.SetSeed(seed);
            settings.RandomLevelSeed = seed;

            RecordMods(api, settings);

            api.BeginRecording(
                settings.LevelSystem?.ID.X ?? 0,
                (int)settings.Mode,
                (int)settings.MatchLength,
                ActiveVariants(settings));

            if (!service.IsRecording)
            {
                return;
            }

            if (settings.Mode == Modes.Trials)
            {
                service.SetTrialsLevel(settings.LevelSystem.ID.X, settings.LevelSystem.ID.Y);
            }

            IsActive = true;
            _isTrials = settings.Mode == Modes.Trials;

            state.SetFrameDriver("TF.Replay");
            state.SetDriverFlags(0, true, false, false, false, 0);

            ServiceCollections.ResolveLogger().LogDebug(
                "Recording {mode} on tower {tower}, seed {seed}", settings.Mode, settings.LevelSystem?.ID.X, seed);
        }

        public static void Step(Level level)
        {
            if (!IsActive || level == null)
            {
                return;
            }

            if (!_consuming)
            {
                if (!HasStarted(level))
                {
                    Warmup();

                    return;
                }

                _consuming = true;
            }

            var halted = _halted;
            var wasPaused = _paused;

            _halted = IsHalted(level);
            _paused = level.Paused;

            if (_paused || wasPaused || (halted && _halted))
            {
                return;
            }

            var state = ServiceCollections.ResolveStateApi();

            byte[] bytes;

            try
            {
                state.StepOwnedControllers();

                state.SetCurrentFrame(_frame);

                bytes = state.GetGameStateBytesForRecording();
            }
            catch (Exception e)
            {
                ServiceCollections.ResolveLogger().LogError("Stopped recording at frame {frame}: {error}", _frame, e);
                Abandon();
                return;
            }

            if (bytes == null)
            {
                return;
            }

            ServiceCollections.ResolveApi().AddRecord(bytes, LiveInputs.Capture(), _frame);

            SnapshotResults(level);

            _frame++;
            state.SetCurrentFrame(_frame);

            if (_isTrials)
            {
                if (state.IsTrialsOver())
                {
                    ArmExport(level, state);
                }

                return;
            }

            if (level.Get<VersusMatchResults>() != null)
            {
                Finish();
            }
        }

        private static void ArmExport(Level level, Interop.ITfStateApi state)
        {
            var api = ServiceCollections.ResolveApi();

            ServiceCollections.ResolveReplayService().SetTrialsTime(state.GetTrialsTime());

            PushResults(api, state, won: level.Get<TowerFall.Dummy>() == null);

            IsActive = false;
            ExportPending = true;

            state.SetFrameDriver(null);
            state.SetDriverFlags(0, false, false, false, false, 0);

            level.Add(new CustomComponent.TrialsExportPrompt());
        }

        public static string ExportNow()
        {
            if (!ExportPending)
            {
                return null;
            }

            ExportPending = false;

            var path = ServiceCollections.ResolveApi().Export();

            Reset();

            return path;
        }

        public static void Finish()
        {
            if (ExportPending)
            {
                ServiceCollections.ResolveApi().ResetRecording();
                Reset();

                return;
            }

            if (!IsActive)
            {
                return;
            }

            var api = ServiceCollections.ResolveApi();
            var state = ServiceCollections.ResolveStateApi();

            if (_isTrials)
            {
                ServiceCollections.ResolveReplayService().SetTrialsTime(state.GetTrialsTime());
            }

            PushResults(api, state);

            var path = api.Export();

            state.SetFrameDriver(null);
            state.SetDriverFlags(0, false, false, false, false, 0);

            Reset();

            if (path != null)
            {
                ServiceCollections.ResolveLogger().LogDebug("Exported {path}", path);
            }
        }

        public static void Reset()
        {
            IsActive = false;
            ExportPending = false;
            _consuming = false;
            _isTrials = false;
            _halted = false;
            _paused = false;
            _frame = 0;

            _seats = null;
            _characters = null;
            _alts = null;
            _teams = null;
            _scores = null;
            _winner = -1;
        }

        private static bool IsHalted(Level level)
        {
            if (level.ReplayViewer != null && level.ReplayViewer.Visible)
            {
                return true;
            }

            var results = level.Get<VersusRoundResults>();

            if (results == null)
            {
                return false;
            }

            var dynResults = DynamicData.For(results);

            return dynResults.Get<bool>("finished") && dynResults.Get<bool>("focused");
        }

        private static bool HasStarted(Level level)
        {
            if (!_isTrials)
            {
                return level.Get<Player>() != null;
            }

            var control = level.Get<TowerFall.TrialsControl>();

            return control != null && control.Started;
        }

        private static void Warmup()
        {
            if (_warm)
            {
                return;
            }

            _warm = true;

            try
            {
                ServiceCollections.ResolveStateApi().GetGameStateBytesForRecording();
            }
            catch
            {

            }
        }

        private static void Abandon()
        {
            var state = ServiceCollections.ResolveStateApi();

            ServiceCollections.ResolveApi().ResetRecording();

            state.SetFrameDriver(null);
            state.SetDriverFlags(0, false, false, false, false, 0);

            Reset();
        }

        private static void SnapshotResults(Level level)
        {
            var session = level.Session;

            if (_seats == null)
            {
                var seats = new List<int>();

                for (int seat = 0; seat < TFGame.Players.Length; seat++)
                {
                    if (TFGame.Players[seat])
                    {
                        seats.Add(seat);
                    }
                }

                _seats = seats.ToArray();
                _characters = _seats.Select(seat => TFGame.Characters[seat]).ToArray();
                _alts = _seats.Select(seat => (int)TFGame.AltSelect[seat]).ToArray();
                _teams = Teams(session?.MatchSettings ?? TowerFall.MainMenu.VersusMatchSettings);
                _scores = new int[_seats.Length];
            }

            for (int i = 0; i < _seats.Length; i++)
            {
                _scores[i] = session?.Scores[_seats[i]] ?? 0;
            }

            var winner = session?.GetWinner() ?? -1;

            if (winner != -1)
            {
                _winner = winner;
            }
        }

        private static void PushResults(Ports.ITfReplayApi api, Interop.ITfStateApi state, bool won = false)
        {
            api.SetSeed(state.GetSeed());

            api.SetLocalSeat(0);

            if (_seats == null)
            {
                return;
            }

            api.SetPlayerCount(_seats.Length);

            api.SetArchersFlat(
                _characters,
                _alts,
                _seats.Select(seat => won || _winner == seat).ToArray(),
                _scores,
                _seats.Select(seat => $"P{seat + 1}").ToArray());

            api.SetArcherTeams(_teams);
        }

        private static int[] Teams(MatchSettings settings)
        {
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

        private static void RecordMods(Ports.ITfReplayApi api, MatchSettings settings)
        {
            var catalog = ServiceCollections.ResolveModCollections();

            if (catalog == null)
            {
                return;
            }

            var state = ServiceCollections.ResolveStateApi();
            var mods = new Dictionary<string, Dictionary<string, string>>();

            foreach (var owner in CustomVariantOwners(settings))
            {
                mods[owner] = new Dictionary<string, string>
                {
                    [Interop.ModData.StateEventsKey] = (state?.HasStateEvents(owner) ?? false).ToString(),
                };
            }

            var widerSet = catalog.ResolveWiderSet();

            if (widerSet != null && widerSet.IsWide)
            {
                mods[Interop.ModData.WiderSetName] = new Dictionary<string, string>
                {
                    [Interop.ModData.IsWideKey] = widerSet.IsWide.ToString(),
                };
            }

            foreach (var mod in mods)
            {
                mod.Value[Interop.ModData.VersionKey] = catalog.GetVersion(mod.Key) ?? "";

                api.AddRecordingMod(mod.Key, mod.Value.Keys.ToArray(), mod.Value.Values.ToArray());
            }
        }

        private static IEnumerable<string> CustomVariantOwners(MatchSettings settings)
        {
            var variants = settings?.Variants?.CustomVariants;

            if (variants == null)
            {
                return [];
            }

            return variants
                .Where(pair => pair.Value != null && pair.Value.Value && pair.Key.Contains('/'))
                .Select(pair => pair.Key.Split('/')[0])
                .Distinct();
        }

        private static string[] ActiveVariants(MatchSettings settings)
        {
            var variants = settings?.Variants;

            if (variants == null)
            {
                return [];
            }

            return variants.Variants.Where(variant => variant.Value).Select(variant => variant.Title)
                .Concat(variants.CustomVariants.Where(pair => pair.Value.Value).Select(pair => pair.Value.Title))
                .Distinct()
                .ToArray();
        }
    }
}
