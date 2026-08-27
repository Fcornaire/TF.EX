using FortRise;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TF.EX.Common;
using TF.EX.Common.Interop;
using TF.EX.Domain.Context;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Domain.Services;
using TF.EX.Domain.Services.TF;

namespace TF.EX.Domain
{
    public static class ServiceCollections
    {
        public static ServiceCollection ServiceCollection;
        public static IServiceProvider ServiceProvider;
        private static HashSet<double> _cachedPickupEntries = new HashSet<double>();
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
        private static IModCollections _modCollections;

        public static void RegisterServices(IModuleContext context, ILogger logger)
        {
            if (ServiceCollection != null)
            {
                throw new Exception("ServiceCollection already registered");
            }

            ServiceCollection = new ServiceCollection();

            ServiceCollection.AddLazyCache();

            ServiceCollection.AddSingleton<IAutoUpdater, AutoUpdater>();
            ServiceCollection.AddSingleton(logger);
            ServiceCollection.AddSingleton(context);

            //TODO: refactor , only game context should be registered as singleton
            ServiceCollection.AddSingleton<IGameContext, GameContext>();
            ServiceCollection.AddSingleton<INetplayManager, NetplayManager>();
            ServiceCollection.AddSingleton<IMatchmakingService, MatchmakingService>();
            ServiceCollection.AddSingleton<ISkinStreamService, SkinStreamService>();
            ServiceCollection.AddSingleton<ISkinOverlayService, SkinOverlayService>();
            ServiceCollection.AddSingleton<IReplayService, ReplayService>();
            ServiceCollection.AddSingleton<ISyncTestUtilsService, SyncTestUtilsService>();

            ServiceCollection.AddTransient<IInputService, InputService>();
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

        public static INetplayManager ResolveNetplayManager() { return ServiceProvider.GetRequiredService<INetplayManager>(); }

        public static IMatchmakingService ResolveMatchmakingService() { return ServiceProvider.GetRequiredService<IMatchmakingService>(); }

        public static IArcherService ResolveArcherService() { return ServiceProvider.GetRequiredService<IArcherService>(); }

        public static IReplayService ResolveReplayService() { return ServiceProvider.GetRequiredService<IReplayService>(); }

        public static IAutoUpdater ResolveAutoUpdater() { return ServiceProvider.GetRequiredService<IAutoUpdater>(); }

        public static ISyncTestUtilsService ResolveSyncTestUtilsService() { return ServiceProvider.GetRequiredService<ISyncTestUtilsService>(); }

        public static IInputService ResolveInputService() { return ServiceProvider.GetRequiredService<IInputService>(); }

        public static ISkinOverlayService ResolveSkinOverlayService() { return ServiceProvider.GetRequiredService<ISkinOverlayService>(); }

        public static void RegisterModCollections(IModCollections modCollections) => _modCollections = modCollections;

        public static IWiderSetModApi ResolveWiderSetModApi() => _modCollections?.ResolveWiderSet();

        public static IModCollections ResolveModCollections() => _modCollections;

        public static ILogger ResolveLogger() { return ServiceProvider.GetRequiredService<ILogger>(); }

        public static IModuleContext ResolveContext()
        {
            return ServiceProvider.GetRequiredService<IModuleContext>();
        }

    }
}
