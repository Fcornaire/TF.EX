using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
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

        public static void RegisterServices()
        {
            if (ServiceCollection != null)
            {
                throw new Exception("ServiceCollection already registered");
            }

            ServiceCollection = new ServiceCollection();

            ServiceCollection.AddMemoryCache();

            ServiceCollection.AddSingleton<IGameContext, GameContext>();
            ServiceCollection.AddSingleton<INetplayManager, NetplayManager>();
            ServiceCollection.AddSingleton<IMatchmakingService, MatchmakingService>();
            ServiceCollection.AddTransient<INetplayStateMachine, DefaultNetplayStateMachine>();
            ServiceCollection.AddSingleton<INetplayStateMachine, Netplay1V1QuickPlayStateMachine>();
            ServiceCollection.AddSingleton<INetplayStateMachine, Netplay1V1DirectStateMachine>();
            ServiceCollection.AddSingleton<IReplayService, ReplayService>();

            ServiceCollection.AddTransient<IInputService, InputService>();
            ServiceCollection.AddTransient<IArrowService, ArrowService>();
            ServiceCollection.AddTransient<ISessionService, SessionService>();
            ServiceCollection.AddTransient<IOrbService, OrbService>();
            ServiceCollection.AddTransient<IRngService, RngService>();
            ServiceCollection.AddTransient<IHUDService, HUDService>();
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

        public static void AddToCache(double actualDepth, Monocle.Entity entity)
        {
            var cache = ServiceProvider.GetService<IMemoryCache>();
            if (!cache.TryGetValue(actualDepth, out Monocle.Entity _))
            {
                cache.Set(actualDepth, entity);
                if (entity is Pickup)
                {
                    _cachedPickupEntries.Add(actualDepth);
                }
            }
        }

        public static T GetCached<T>(double actualDepth) where T : Monocle.Entity
        {
            var cache = ServiceProvider.GetService<IMemoryCache>();

            if (cache.TryGetValue(actualDepth, out T cached))
            {
                return cached;
            }

            return null;
        }

        public static INetplayManager ResolveNetplayManager() { return ServiceProvider.GetRequiredService<INetplayManager>(); }
        public static ISessionService ResolveSessionService() { return ServiceProvider.GetRequiredService<ISessionService>(); }

        public static IMatchmakingService ResolveMatchmakingService() { return ServiceProvider.GetRequiredService<IMatchmakingService>(); }

        public static IRngService ResolveRngService() { return ServiceProvider.GetRequiredService<IRngService>(); }

        public static IReplayService ResolveReplayService() { return ServiceProvider.GetRequiredService<IReplayService>(); }

        public static IInputService ResolveInputService() { return ServiceProvider.GetRequiredService<IInputService>(); }

        public static IHUDService ResolveHUDService() { return ServiceProvider.GetRequiredService<IHUDService>(); }

        public static IArrowService ResolveArrowService() { return ServiceProvider.GetRequiredService<IArrowService>(); }
        public static IOrbService ResolveOrbService() { return ServiceProvider.GetRequiredService<IOrbService>(); }

        public static (INetplayStateMachine, TF.EX.Domain.Models.Modes) ResolveStateMachineService()
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            var implem = mode.ToType();
            var stateMachines = ServiceProvider.GetServices<INetplayStateMachine>();

            return (stateMachines.First(service => service.GetType() == implem), mode);
        }

        public static void ResetState()
        {
            var sessionService = ResolveSessionService();
            var hudService = ServiceProvider.GetRequiredService<IHUDService>();
            PurgeCachedPickup();
            sessionService.Reset();
            hudService.Update(new Domain.Models.State.HUD.HUD());
        }

        /// <summary>
        /// Caching pickup is a bit tricky since between rounds the cached pickup might not be the same as the one that is spawned.
        /// <para>For now, removing pickup between round is a temporary solution</para>
        /// </summary>
        private static void PurgeCachedPickup()
        {
            var cache = ServiceProvider.GetService<IMemoryCache>();

            foreach (var cachedPickup in _cachedPickupEntries)
            {
                cache.Remove(cachedPickup);
            }
        }
    }
}
