using Microsoft.Extensions.DependencyInjection;
using TF.EX.Patchs.Calc;
using TF.EX.Patchs.Commands;
using TF.EX.Patchs.Component;
using TF.EX.Patchs.Engine;
using TF.EX.Patchs.Entity;
using TF.EX.Patchs.Entity.HUD;
using TF.EX.Patchs.Entity.LevelEntity;
using TF.EX.Patchs.Entity.MenuItem;
using TF.EX.Patchs.Layer;
using TF.EX.Patchs.PlayerInput;
using TF.EX.Patchs.RoundLogic;
using TF.EX.Patchs.Scene;

namespace TF.EX.Patchs
{
    public static class ServiceProviderExtensions
    {
        public static void RegisterPatchs(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IHookable, FightButtonPatch>();
            serviceCollection.AddSingleton<IHookable, CommandsPatch>();
            serviceCollection.AddSingleton<IHookable, TFGamePatch>();
            serviceCollection.AddSingleton<IHookable, CalcPatch>();
            serviceCollection.AddSingleton<IHookable, ArrowCushionPatch>();
            serviceCollection.AddSingleton<IHookable, PlayerIndicatorPatch>();
            serviceCollection.AddSingleton<IHookable, VersusMatchResultsPatch>();
            serviceCollection.AddSingleton<IHookable, BladeButtonPatch>();
            serviceCollection.AddSingleton<IHookable, OptionsButtonPatch>();
            serviceCollection.AddSingleton<IHookable, RollCallElementPatch>();
            serviceCollection.AddSingleton<IHookable, VersusBeginButtonPatch>();
            serviceCollection.AddSingleton<IHookable, VersusVariantButtonPatch>();
            serviceCollection.AddSingleton<IHookable, KeyboardInputPatch>();
            serviceCollection.AddSingleton<IHookable, XGamePadInputPatch>();
            serviceCollection.AddSingleton<IHookable, LevelTestRoundLogicPatch>();
            serviceCollection.AddSingleton<IHookable, LastManStandingRoundLogicPatch>();
            serviceCollection.AddSingleton<IHookable, RoundLogicPatch>();
            serviceCollection.AddSingleton<IHookable, LevelPatch>();
            serviceCollection.AddSingleton<IHookable, LavaPatch>();
            serviceCollection.AddSingleton<IHookable, LavaControlPatch>();
            serviceCollection.AddSingleton<IHookable, PickupPatch>();
            serviceCollection.AddSingleton<IHookable, TreasureSpawnerPatch>();
            serviceCollection.AddSingleton<IHookable, VersusRoundResultsPatch>();
            serviceCollection.AddSingleton<IHookable, ArrowPatch>();
            serviceCollection.AddSingleton<IHookable, CoroutinePatch>();
            serviceCollection.AddSingleton<IHookable, EntityPatch>();
            serviceCollection.AddSingleton<IHookable, GameplayLayerPatch>();
            serviceCollection.AddSingleton<IHookable, InputRendererPatch>();
            serviceCollection.AddSingleton<IHookable, LayerPatch>();
            serviceCollection.AddSingleton<IHookable, PlayerPatch>();
            serviceCollection.AddSingleton<IHookable, OrbLogicPatch>();
            serviceCollection.AddSingleton<IHookable, LevelLoaderXMLPatch>();
            serviceCollection.AddSingleton<IHookable, MainMenuPatch>();
            serviceCollection.AddSingleton<IHookable, PauseMenuPatch>();
            serviceCollection.AddSingleton<IHookable, SessionPatch>();
            serviceCollection.AddSingleton<IHookable, VersusLevelSystemPatch>();
            serviceCollection.AddSingleton<IHookable, VersusStartPatch>();
            serviceCollection.AddSingleton<IHookable, ReplayRecorderPatch>();
            serviceCollection.AddSingleton<IHookable, ChainPatch>();
            serviceCollection.AddSingleton<IHookable, MatchSettingsPatch>();
            serviceCollection.AddSingleton<IHookable, HUDPatch>();
            serviceCollection.AddSingleton<IHookable, VersusCoinButtonPatch>();
            serviceCollection.AddSingleton<IHookable, SaveDataPatch>();
        }

        public static LavaPatch GetLavaPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is LavaPatch) as LavaPatch;
        }

        public static PlayerPatch GetPlayerPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is PlayerPatch) as PlayerPatch;
        }

        public static ArrowPatch GetArrowPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is ArrowPatch) as ArrowPatch;
        }

        public static PickupPatch GetPickupPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is PickupPatch) as PickupPatch;
        }

        public static OrbLogicPatch GetOrbLogicPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is OrbLogicPatch) as OrbLogicPatch;
        }

        public static LevelPatch GetLevelPatch(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetServices<IHookable>().First(service => service is LevelPatch) as LevelPatch;
        }

        public static void LoadPatchs(this IServiceProvider serviceProvider)
        {
            foreach (var hookable in serviceProvider.GetServices<IHookable>())
            {
                hookable.Load();
            }
        }

        public static void UnloadPatchs(this IServiceProvider serviceProvider)
        {
            foreach (var hookable in serviceProvider.GetServices<IHookable>())
            {
                hookable.Unload();
            }
        }
    }
}
