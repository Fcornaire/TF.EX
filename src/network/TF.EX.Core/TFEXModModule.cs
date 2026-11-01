using FortRise;
using Microsoft.Extensions.Logging;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TF.EX.Core.RoundLogic;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TF.EX.Domain.Utils;
using TowerFall;

namespace TF.EX
{
    internal class TFEXModModule : Mod
    {
        public static TFEXModModule Instance;
        public const string ModName = "TF.EX";

        public static ISubtextureEntry InternetIcon { get; private set; } = null!;
        public static IVariantEntry RightStickVariant { get; private set; } = null!;
        private List<TutorialSlide> _tutorialSlides = [];


        public TFEXModModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
        {
            Instance = this;

            RegisterAndLoad(context, content, logger);
            OnVariantsRegister(context);

            OnUnload = Unload;
        }

        public override ModuleSettings CreateSettings() => new NetplaySettings();

        private readonly Core.Api.TfExApi _api = new();

        public override object GetApi() => _api;

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
                message => TF.EX.Domain.CustomComponent.Notification.CreateOrDefer(TowerFall.TFGame.Instance.Scene, message),
                (replayFileName, currentSong) => replayService.LoadAndStart(replayFileName, currentSong).GetAwaiter().GetResult());
        }

        private void ConfigureChangelog(IModContent content)
        {
            var settings = GetSettings<NetplaySettings>();

            if (settings == null)
            {
                return;
            }

            var markdown = content.Root.TryGetRelativePath("CHANGELOG.md", out var file) ? file.Text : "";

            TF.EX.Domain.Utils.Changelog.Configure(
                markdown,
                content.Metadata.Version.ToString(),
                settings.LastVersionSeen,
                seen => settings.LastVersionSeen = seen);
        }

        private void ConfigureTutorial()
        {
            var settings = GetSettings<NetplaySettings>();

            if (settings == null)
            {
                return;
            }

            if (!settings.HasSeenTutorial && IsReturningPlayer())
            {
                settings.HasSeenTutorial = true;
            }

            Tutorial.Configure(_tutorialSlides, !settings.HasSeenTutorial, () => settings.HasSeenTutorial = true);

            if (Tutorial.IsPending)
            {
                Changelog.MarkSeen();
            }
        }

        private static bool IsReturningPlayer()
        {
            var savesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            var replaysRoot = Path.Combine(savesRoot, "TF.Replay", "Replays");

            return File.Exists(Path.Combine(savesRoot, ModName, $"{ModName}.settings.json"))
                || (Directory.Exists(replaysRoot) && Directory.EnumerateFiles(replaysRoot, "*.tow", SearchOption.AllDirectories).Any());
        }

        private static TutorialSlide CreateTutorialSlide(IModuleContext context, IModContent content, string image, string textA, string textB, int dir)
        {
            var texture = context.Registry.Subtextures.RegisterTexture(content.Root.GetRelativePath($"imgs/tutorial/{image}.png"));

            return new TutorialSlide
            {
                TextA = textA,
                TextB = textB,
                Dir = dir,
                Image = () => texture.Subtexture,
            };
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
                content.Root.GetRelativePath("imgs/netplay.png")
            );

            TF.EX.Domain.CustomComponent.MenuIcons.ConfigureOnline(() => InternetIcon.Subtexture);

            _tutorialSlides =
            [
                CreateTutorialSlide(context, content, "welcome", "WELCOME TO TOWERFALL EX!", "GO ONLINE FROM THE NETPLAY BUTTON", 1),
                CreateTutorialSlide(context, content, "name", "PICK YOUR PLAYER NAME IN", "MODS / TF.EX / NETPLAY NAME", -1),
                CreateTutorialSlide(context, content, "options", "MORE EX OPTIONS IN MODS", "INPUT DELAY, ROLLBACK, SKINS...", 1),
                CreateTutorialSlide(context, content, "netplayEntry", "ENJOY THE RIDE!", "", -1),
            ];

            typeof(TF.EX.API.ModExports).ModInterop();

            var netplayGameMode = context.Registry.GameModes.RegisterVersusGameMode(new NetplayVersusMode());
            TF.EX.Domain.Extensions.VersusModeExtensions.SetNetplayMode(netplayGameMode.Modes);
            TF.EX.Patchs.MatchVariantsPatchs.OwnModName = Meta.Name;

            var commands = new TF.EX.Core.TFCommands();
            commands.Register(context);

            TF.EX.Domain.ServiceCollections.RegisterServices(context, content.Metadata, logger);
            TF.EX.Domain.ServiceCollections.Build();

            var mods = new TF.EX.Core.Api.ModCollections(context, logger);

            TF.EX.Domain.ServiceCollections.RegisterModCollections(mods);
            TF.EX.Domain.Interop.ArcherRegistryApi.Configure(context.Registry.Archers);
            TF.EX.Domain.Interop.ModRegistryApi.Configure(context.Registry, Meta.Name);
            TF.EX.Domain.Interop.StateApi.Configure(mods.ResolveState);
            TF.EX.Domain.Interop.ReplayApi.Configure(mods.ResolveReplay);
            TF.EX.Domain.Interop.InputDisplayerApi.Configure(mods.ResolveInputDisplayer);

            OnInitialize = _ =>
            {
                InitializeApis();
                ConfigureChangelog(content);
                ConfigureTutorial();
            };

            context.Harmony.PatchAll(typeof(Patchs.Engine.TFGamePatch).Assembly);

            Patchs.Scene.WiderSetMenu.PatchSelectionButtons(context.Harmony);
        }
    }
}
