using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TF.Replay.Domain.Ports;

namespace TF.Replay.Domain
{
    public static class ServiceCollections
    {
        private static ILogger _logger;
        private static IReplayService _replayService;
        private static ITfReplayApi _api;
        private static Func<Interop.IModCollections> _modCollections;

        private static Action<bool> _setInputEnabled;
        private static Action _ensureFakeControllers;
        private static Action<string> _notify;
        private static Func<string, string, string> _launchReplay;

        public static void Register(ILogger logger, IReplayService replayService, ITfReplayApi api,
                                    Func<Interop.IModCollections> modCollections = null)
        {
            _logger = logger;
            _replayService = replayService;
            _api = api;
            _modCollections = modCollections;
        }

        public static void RegisterHost(Action<bool> setInputEnabled, Action ensureFakeControllers,
                                        Action<string> notify, Func<string, string, string> launchReplay)
        {
            _setInputEnabled = setInputEnabled;
            _ensureFakeControllers = ensureFakeControllers;
            _notify = notify;
            _launchReplay = launchReplay;
        }

        public static ILogger ResolveLogger() => _logger ?? NullLogger.Instance;

        public static IReplayService ResolveReplayService() => _replayService;

        public static ITfReplayApi ResolveApi() => _api;

        public static Interop.IModCollections ResolveModCollections() => _modCollections?.Invoke();

        public static Interop.ITfStateApi ResolveStateApi() => ResolveModCollections()?.ResolveState();

        public static void SetInputEnabled(bool enabled)
        {
            if (_setInputEnabled != null)
            {
                _setInputEnabled(enabled);
                return;
            }

            ReplayInputGate.SetInputEnabled(enabled);
        }

        public static void EnsureFakeControllers() => _ensureFakeControllers?.Invoke();

        public static void Notify(string message)
        {
            if (_notify != null)
            {
                _notify(message);
                return;
            }

            ResolveLogger().LogWarning("{Message}", message);
        }

        public static string LaunchReplay(string replayFileName, string currentSong)
        {
            if (_launchReplay != null)
            {
                return _launchReplay(replayFileName, currentSong);
            }

            var api = ResolveApi();

            return api == null ? "TF.REPLAY IS NOT READY" : api.StartPlayback(replayFileName);
        }

        public static void Reset()
        {
            _logger = null;
            _replayService = null;
            _api = null;
            RegisterHost(null, null, null, null);
        }
    }
}
