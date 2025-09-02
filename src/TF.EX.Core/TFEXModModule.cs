using FortRise;
using Microsoft.Extensions.Logging;
using Monocle;
using TF.EX.API;
using TF.EX.Core.RoundLogic;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TF.EX.Patchs;
using TowerFall;

namespace TF.EX
{
    internal class TFEXModModule : Mod
    {
        public static TFEXModModule Instance;
        public static Atlas Atlas;
        public static ISubtextureEntry InternetIcon { get; private set; } = null!;
        public static IVariantEntry RightStickVariant { get; private set; } = null!;


        public TFEXModModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
        {
            Instance = this;

            RegisterAndLoad(context, content, logger);
            OnVariantsRegister(context);

            OnUnload = Unload;
        }

        //public override void LoadContent()
        //{
        //    Atlas = Content.LoadAtlas("Atlas/atlas.xml", "Atlas/atlas.png", true);
        //}

        //public override void Load()
        //{
        //    RegisterAndLoad();
        //}

        public void Unload(IModuleContext context)
        {
            if (TFGame.Instance.Scene is Level && ServiceCollections.ResolveNetplayManager().IsServerMode())
            {
                ServiceCollections.ResolveReplayService().Export();
            }

            //context.Harmony.Unpatch()
        }

        private void OnVariantsRegister(IModuleContext context)
        {
            //var rightStickArrow = new CustomVariantInfo(
            //    Constants.RIGHT_STICK_VARIANT_NAME, Atlas["variants/freeAiming"],
            //    "Shoot arrows with your right stick!".ToUpperInvariant(),
            //    CustomVariantFlags.CanRandom
            //);

            var icon = context.Registry.Subtextures.RegisterTexture(() => TFGame.MenuAtlas["variants/freeAiming"]);

            RightStickVariant = context.Registry.Variants.RegisterVariant(Constants.RIGHT_STICK_VARIANT_NAME, new()
            {
                Title = Constants.RIGHT_STICK_VARIANT_NAME,
                Icon = icon,
                Flags = CustomVariantFlags.PerPlayer
            });

            //manager.AddVariant(rightStickArrow, noPerPlayer);
        }

        private void RegisterAndLoad(IModuleContext context, IModContent content, ILogger logger)
        {
            //typeof(ModExports).ModInterop();

            InternetIcon = context.Registry.Subtextures.RegisterTexture(
                content.Root.GetRelativePath("imgs/icons8-internet-48.png")
            );

            context.Registry.GameModes.RegisterVersusGameMode(new NetplayVersusMode());

            var commands = new TF.EX.Core.TFCommands();
            commands.Register(context);

            TF.EX.Domain.ServiceCollections.RegisterServices(context, logger);
            TF.EX.Domain.ServiceCollections.ServiceCollection.RegisterPatchs();
            TF.EX.Domain.ServiceCollections.ServiceCollection.AddAPIManager();
            TF.EX.Domain.ServiceCollections.Build();
            //TF.EX.Domain.ServiceCollections.ServiceProvider.LoadPatchs();

            context.Harmony.PatchAll(typeof(Patchs.Engine.TFGamePatch).Assembly);
        }
    }

    //public class RightStickTexture : ISubtextureEntry
    //{
    //    private Subtexture texture { get; }

    //    public RightStickTexture(Subtexture texture)
    //    {
    //        this.texture = texture;
    //    }

    //    public string ID { get => "RightStickArrow"; init => throw new System.NotImplementedException(); }
    //    public IResourceInfo Path { get => throw new System.NotImplementedException(); init => throw new System.NotImplementedException(); }

    //    public Subtexture Subtexture => texture;

    //    public SubtextureAtlasDestination AtlasDestination => throw new System.NotImplementedException();
    //}
}
