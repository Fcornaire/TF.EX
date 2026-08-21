using Microsoft.Extensions.Logging;
using TowerFall;

namespace TF.EX.Domain
{
    public static class ScenarioSweeper
    {
        private readonly record struct Run(string Label, Action Launch, int Frames, Func<Level, bool> Expect);

        private const int TEARDOWN_FRAMES = 10;

        private static readonly Queue<Run> _queue = new Queue<Run>();
        private static readonly List<string> _results = new List<string>();

        private static Action _teardown;
        private static Action _onFinished;
        private static int _framesPerRun;
        private static int _stallLimit;
        private static int _elapsed;
        private static int _sinceLaunch;
        private static int _cooldown;
        private static bool _pendingLaunch;
        private static bool _expectMet;
        private static Level _lastLevel;

        private static readonly Dictionary<string, int> _peak = new Dictionary<string, int>();
        private static Run? _current;

        public static bool IsRunning => _current != null;

        public static void Start(IEnumerable<(string Label, Action Launch, int Frames, Func<Level, bool> Expect)> runs, int defaultFrames, Action teardown, Action onFinished)
        {
            _queue.Clear();
            _results.Clear();

            foreach (var run in runs)
            {
                _queue.Enqueue(new Run(run.Label, run.Launch, run.Frames > 0 ? run.Frames : defaultFrames, run.Expect));
            }

            _teardown = teardown;
            _onFinished = onFinished;
            _current = null;

            Next();
        }

        public static void Update()
        {
            if (!_pendingLaunch)
            {
                return;
            }

            if (_cooldown > 0)
            {
                _cooldown--;

                return;
            }

            if (TFGame.Instance?.Scene is not TowerFall.MainMenu)
            {
                return;
            }

            _pendingLaunch = false;
            _elapsed = 0;
            _sinceLaunch = 0;
            _expectMet = false;

            _framesPerRun = _current.Value.Frames;
            _stallLimit = _framesPerRun * 2 + 600;

            _current.Value.Launch();
        }

        public static void Tick(Level level)
        {
            if (_current == null || _pendingLaunch)
            {
                return;
            }

            _sinceLaunch++;

            if (level != null)
            {
                _elapsed++;
                _lastLevel = level;

                if (level.Session?.RoundLogic?.RoundStarted == true)
                {
                    InputScripter.MarkRoundBegun();
                }

                if (_elapsed % 15 == 0)
                {
                    SamplePeak(level);
                }

                if (!_expectMet && _current.Value.Expect != null)
                {
                    _expectMet = _current.Value.Expect(level);
                }
            }

            if (level != null && IsRoundOver(level))
            {
                Finish($"round ended at frame {_elapsed}");

                return;
            }

            if (_elapsed >= _framesPerRun)
            {
                Finish($"{_framesPerRun} frames");
            }

            if (_sinceLaunch >= _stallLimit)
            {
                Record($"STALLED (only {_elapsed}/{_framesPerRun} frames)");
                Next();
            }
        }

        private static void SamplePeak(Level level)
        {
            foreach (var group in level.Layers.SelectMany(layer => layer.Value.Entities).GroupBy(e => e.GetType().Name))
            {
                if (!_peak.TryGetValue(group.Key, out var best) || group.Count() > best)
                {
                    _peak[group.Key] = group.Count();
                }
            }
        }

        private static bool IsRoundOver(Level level)
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities).Any(e => e is VersusRoundResults);
        }

        private static void Finish(string how)
        {
            var met = _expectMet || _current.Value.Expect == null;

            Record(met ? $"PASS ({how})" : $"EXPECTATION NOT MET ({how})");

            if (!met && _lastLevel != null)
            {
                var census = _lastLevel.Layers
                    .SelectMany(layer => layer.Value.Entities)
                    .GroupBy(e => e.GetType().Name)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Count() <= 4
                        ? $"{g.Key} x{g.Count()} @{string.Join("/", g.Select(e => $"{(int)e.X},{(int)e.Y}"))}"
                        : $"{g.Key} x{g.Count()}");

                var logger = ServiceCollections.ResolveLogger();

                logger.LogInformation($"[sweep]   level held: {string.Join(", ", census)}");
                logger.LogInformation($"[sweep]   peak seen: {string.Join(", ", _peak.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} x{kv.Value}"))}");
            }

            Next();
        }

        public static void OnDesync(int frame)
        {
            if (_current == null || _pendingLaunch)
            {
                return;
            }

            Record($"DESYNC at frame {frame}");
            Next();
        }

        public static void Abort(string reason)
        {
            if (_current == null)
            {
                return;
            }

            Record($"ABORTED ({reason})");
            _queue.Clear();
            Next();
        }

        private static void Record(string outcome)
        {
            var line = $"[sweep] {_current.Value.Label} : {outcome}";

            _results.Add(line);
            ServiceCollections.ResolveLogger().LogInformation(line);
        }

        private static void Next()
        {
            _elapsed = 0;
            _sinceLaunch = 0;
            _expectMet = false;
            _lastLevel = null;
            _peak.Clear();
            _pendingLaunch = false;

            if (_queue.Count == 0)
            {
                _current = null;
                InputScripter.Stop();

                var logger = ServiceCollections.ResolveLogger();
                var failed = _results.Count(r => !r.Contains(": PASS"));

                logger.LogInformation($"[sweep] --- finished : {_results.Count - failed}/{_results.Count} PASS ---");
                foreach (var result in _results)
                {
                    logger.LogInformation(result);
                }

                _onFinished?.Invoke();

                return;
            }

            _current = _queue.Dequeue();

            _pendingLaunch = true;
            _cooldown = TEARDOWN_FRAMES;

            _teardown?.Invoke();
        }
    }
}
