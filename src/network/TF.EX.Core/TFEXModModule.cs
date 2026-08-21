using FortRise;
using Microsoft.Extensions.Logging;
using Monocle;
using TF.EX.Core.RoundLogic;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX
{
    internal class TFEXModModule : Mod
    {
        public static TFEXModModule Instance;
        public const string ModName = "TF.EX";

        public static ISubtextureEntry InternetIcon { get; private set; } = null!;
        public static IVariantEntry RightStickVariant { get; private set; } = null!;


        public TFEXModModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
        {
            Instance = this;

            RegisterAndLoad(context, content, logger);
            OnVariantsRegister(context);

            OnUnload = Unload;
        }

        public override ModuleSettings CreateSettings() => new NetplaySettings();

        private void InitializeApis()
        {
            var replayService = TF.EX.Domain.ServiceCollections.ResolveReplayService();
            var inputService = TF.EX.Domain.ServiceCollections.ResolveInputService();

            TF.EX.Domain.Interop.ReplayApi.Current.SetRecordDriver(ModName);
            replayService.RegisterPlaybackCallbacks();

            TF.EX.Domain.Interop.ReplayApi.Current.SetHostCallbacks(
                enabled =>
                {
                    if (enabled)
                    {
                        inputService.EnableAllControllers();
                        return;
                    }

                    inputService.DisableAllControllers();
                },
                () => inputService.EnsureFakeControllers(),
                message => TF.EX.Domain.CustomComponent.Notification.Create(TowerFall.TFGame.Instance.Scene, message),
                (replayFileName, currentSong) => replayService.LoadAndStart(replayFileName, currentSong).GetAwaiter().GetResult());
        }

        public void Unload(IModuleContext context)
        {
            if (TFGame.Instance.Scene is Level && ServiceCollections.ResolveNetplayManager().IsServerMode())
            {
                ServiceCollections.ResolveReplayService().Export();
            }

            TF.EX.Domain.Interop.StateApi.Current?.SetFrameDriver(null);
            TF.EX.Domain.Interop.ReplayApi.Current?.SetRecordDriver(null);
        }

        private void OnVariantsRegister(IModuleContext context)
        {
            var icon = context.Registry.Subtextures.RegisterTexture(() => TFGame.MenuAtlas["variants/freeAiming"]);

            RightStickVariant = context.Registry.Variants.RegisterVariant(Constants.RIGHT_STICK_VARIANT_NAME, new()
            {
                Title = Constants.RIGHT_STICK_VARIANT_NAME,
                Icon = icon,
                Flags = CustomVariantFlags.PerPlayer
            });
        }

        private void RegisterAndLoad(IModuleContext context, IModContent content, ILogger logger)
        {
            InternetIcon = context.Registry.Subtextures.RegisterTexture(
                content.Root.GetRelativePath("imgs/icons8-internet-48.png")
            );

            TF.EX.Domain.CustomComponent.MenuIcons.ConfigureOnline(() => InternetIcon.Subtexture);

            context.Registry.GameModes.RegisterVersusGameMode(new NetplayVersusMode());

            var commands = new TF.EX.Core.TFCommands();
            commands.Register(context);

            TF.EX.Domain.ServiceCollections.RegisterServices(context, logger);
            TF.EX.Domain.ServiceCollections.Build();

            var mods = new TF.EX.Core.Api.ModCollections(context, logger);

            TF.EX.Domain.ServiceCollections.RegisterModCollections(mods);
            TF.EX.Domain.Interop.StateApi.Configure(mods.ResolveState);
            TF.EX.Domain.Interop.ReplayApi.Configure(mods.ResolveReplay);

            OnInitialize = _ => InitializeApis();

            context.Harmony.PatchAll(typeof(Patchs.Engine.TFGamePatch).Assembly);

            Patchs.Scene.WiderSetMenu.PatchSelectionButtons(context.Harmony);
        }
    }
}
