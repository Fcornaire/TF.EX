﻿using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using TF.EX.Common;
using TF.EX.Common.Logging;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Domain.Services;
using TF.EX.Domain.Services.StateMachine;
using TF.EX.Domain.Services.TF;
using TowerFall;

namespace TF.EX.Domain
{
    public static class ServiceCollections
    {
        public static ServiceCollection ServiceCollection;
        public static IServiceProvider ServiceProvider;
        private static HashSet<double> _cachedPickupEntries = new HashSet<double>();
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
        public static readonly ReplayVersion CurrentReplayVersion = ReplayVersion.V2;

        public static void RegisterServices()
        {
            if (ServiceCollection != null)
            {
                throw new Exception("ServiceCollection already registered");
            }

            ServiceCollection = new ServiceCollection();

            ServiceCollection.AddLazyCache();

            ServiceCollection.AddSingleton<IAutoUpdater, AutoUpdater>();
            ServiceCollection.AddSingleton<ILogger, Logger>();

            //TODO: refactor , only game context should be registered as singleton
            ServiceCollection.AddSingleton<IGameContext, GameContext>();
            ServiceCollection.AddSingleton<INetplayManager, NetplayManager>();
            ServiceCollection.AddSingleton<IMatchmakingService, MatchmakingService>();
            ServiceCollection.AddTransient<INetplayStateMachine, DefaultNetplayStateMachine>();
            ServiceCollection.AddSingleton<INetplayStateMachine, Netplay1V1QuickPlayStateMachine>();
            ServiceCollection.AddSingleton<INetplayStateMachine, Netplay1V1DirectStateMachine>();
            ServiceCollection.AddSingleton<IReplayService, ReplayService>();
            ServiceCollection.AddSingleton<ISFXService, SFXService>();
            ServiceCollection.AddSingleton<ISyncTestUtilsService, SyncTestUtilsService>();

            ServiceCollection.AddTransient<IInputService, InputService>();
            ServiceCollection.AddTransient<IArrowService, ArrowService>();
            ServiceCollection.AddTransient<ISessionService, SessionService>();
            ServiceCollection.AddTransient<IRngService, RngService>();
            ServiceCollection.AddTransient<IHUDService, HUDService>();
            ServiceCollection.AddTransient<IArcherService, ArcherService>();
        }

        public static void Build()
        {
            if (ServiceCollection == null)
            {
                throw new Exception("ServiceCollection not registered");
            }

            if (ServiceProvider != null)
            {
                throw new Exception("ServiceProvider already registered");
            }

            ServiceProvider = ServiceCollection.BuildServiceProvider();
        }

        public static void AddEntityToCache(double actualDepth, Monocle.Entity entity)
        {
            var cache = ServiceProvider.GetService<IAppCache>();

            cache.Add(actualDepth.ToString(), entity, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                ExpirationTokens = { new CancellationChangeToken(_resetCacheToken.Token) }
            });

            if (entity is Pickup)
            {
                _cachedPickupEntries.Add(actualDepth);
            }

        }

        public static void AddToCache<V>(string key, V value, TimeSpan expires = default)
        {
            var cache = ServiceProvider.GetService<IAppCache>();
            var memoryOptions = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.High,
                ExpirationTokens = { new CancellationChangeToken(_resetCacheToken.Token) }
            };

            if (expires != default)
            {
                memoryOptions.AbsoluteExpirationRelativeToNow = expires;
            }

            cache.Add(key, value, memoryOptions);
        }

        public static T GetCachedEntity<T>(double actualDepth) where T : Monocle.Entity
        {
            var cache = ServiceProvider.GetService<IAppCache>();

            var cached = cache.Get<T>(actualDepth.ToString());

            return cached;
        }

        public static bool GetCached<T>(string key, out T cached) where T : class
        {
            var cache = ServiceProvider.GetService<IAppCache>();

            if (cache.TryGetValue(key, out T c))
            {
                cached = c;
                return true;
            }

            cached = null;
            return false;
        }

        public static INetplayManager ResolveNetplayManager() { return ServiceProvider.GetRequiredService<INetplayManager>(); }
        public static ISessionService ResolveSessionService() { return ServiceProvider.GetRequiredService<ISessionService>(); }

        public static IMatchmakingService ResolveMatchmakingService() { return ServiceProvider.GetRequiredService<IMatchmakingService>(); }

        public static IRngService ResolveRngService() { return ServiceProvider.GetRequiredService<IRngService>(); }

        public static IReplayService ResolveReplayService() { return ServiceProvider.GetRequiredService<IReplayService>(); }

        public static IInputService ResolveInputService() { return ServiceProvider.GetRequiredService<IInputService>(); }

        public static IHUDService ResolveHUDService() { return ServiceProvider.GetRequiredService<IHUDService>(); }

        public static IArrowService ResolveArrowService() { return ServiceProvider.GetRequiredService<IArrowService>(); }
        public static ISFXService ResolveSFXService() { return ServiceProvider.GetRequiredService<ISFXService>(); }

        public static ILogger ResolveLogger() { return ServiceProvider.GetRequiredService<ILogger>(); }


        public static (INetplayStateMachine, TF.EX.Domain.Models.Modes) ResolveStateMachineService()
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            var implem = mode.ToType();
            var stateMachines = ServiceProvider.GetServices<INetplayStateMachine>();

            return (stateMachines.First(service => service.GetType() == implem), mode);
        }

        public static void ResetState()
        {
            var hudService = ServiceProvider.GetRequiredService<IHUDService>();
            PurgeCachedPickup();
            hudService.Update(new Domain.Models.State.Entity.HUD.HUD());
        }

        /// <summary>
        /// Caching pickup is a bit tricky since between rounds the cached pickup might not be the same as the one that is spawned.
        /// <para>For now, removing pickup between round is a temporary solution</para>
        /// </summary>
        private static void PurgeCachedPickup()
        {
            var cache = ServiceProvider.GetService<IAppCache>();

            foreach (var cachedPickup in _cachedPickupEntries) //TODO: Should also remove the cached pickup from the cache 
            {
                cache.Remove(cachedPickup.ToString());
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
