using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TF.State.Domain.Context;
using TF.State.Domain.Ports;
using TF.State.Domain.Services;

namespace TF.State.Domain
{
    public static class ServiceCollections
    {
        private static readonly object _lock = new object();

        private static IStateContext _stateContext;
        private static IRngService _rngService;
        private static ISessionService _sessionService;
        private static IHUDService _hudService;
        private static ISFXService _sfxService;
        private static IMatchStatsService _matchStatsService;
        private static IAPIManager _apiManager;
        private static IAppCache _cache;
        private static ILogger _logger;

        private static HashSet<double> _cachedPickupEntries = new HashSet<double>();
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();

        public static void SetLogger(ILogger logger) => _logger = logger;
        public static ILogger ResolveLogger() => _logger;

        public static IStateContext ResolveStateContext() => _stateContext ??= new StateContext();

        public static IRngService ResolveRngService() => _rngService ??= new RngService(ResolveStateContext());
        public static ISessionService ResolveSessionService() => _sessionService ??= new SessionService(ResolveStateContext());
        public static IHUDService ResolveHUDService() => _hudService ??= new HUDService(ResolveStateContext());
        public static ISFXService ResolveSFXService() => _sfxService ??= new SFXService(ResolveStateContext());
        public static IMatchStatsService ResolveMatchStatsService() => _matchStatsService ??= new MatchStatsService();
        public static IAPIManager ResolveAPIManager() => _apiManager ??= new APIManager();

        private static IAppCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    lock (_lock)
                    {
                        _cache ??= new CachingService();
                    }
                }

                return _cache;
            }
        }

        public static void AddEntityToCache(double actualDepth, Monocle.Entity entity)
        {
            Cache.Add(actualDepth.ToString(), entity, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                ExpirationTokens = { new CancellationChangeToken(_resetCacheToken.Token) }
            });

            if (entity is TowerFall.Pickup)
            {
                _cachedPickupEntries.Add(actualDepth);
            }
        }

        public static void AddToCache<V>(string key, V value, TimeSpan expires = default)
        {
            var memoryOptions = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.High,
                ExpirationTokens = { new CancellationChangeToken(_resetCacheToken.Token) }
            };

            if (expires != default)
            {
                memoryOptions.AbsoluteExpirationRelativeToNow = expires;
            }

            Cache.Add(key, value, memoryOptions);
        }

        public static T GetCachedEntity<T>(double actualDepth) where T : Monocle.Entity
        {
            return Cache.Get<T>(actualDepth.ToString());
        }

        public static bool GetCached<T>(string key, out T cached) where T : class
        {
            if (Cache.TryGetValue(key, out T c))
            {
                cached = c;
                return true;
            }

            cached = null;
            return false;
        }

        public static void ResetState()
        {
            PurgeCachedPickup();
            ResolveHUDService().Update(new Models.Entity.HUD.HUD());
            ResolveSFXService().ClearSnapshots();
            ResolveMatchStatsService().Reset();
        }

        private static void PurgeCachedPickup()
        {
            foreach (var cachedPickup in _cachedPickupEntries)
            {
                Cache.Remove(cachedPickup.ToString());
            }
        }

        public static void PurgeCache()
        {
            if (_resetCacheToken != null && !_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
            _cachedPickupEntries.Clear();
        }
    }
}
