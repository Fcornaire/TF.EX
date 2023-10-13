using FortRise;
using Monocle;
using MonoMod.ModInterop;
using TF.EX.API;
using TF.EX.Domain;
using TF.EX.Patchs;
using TowerFall;

namespace TF.EX
{
    [Fort("dshad.tf.ex", "TF.EX Mod")]
    internal class TFEXModModule : FortModule
    {
        public static TFEXModModule Instance;
        public static Atlas Atlas;

        public TFEXModModule()
        {
            Instance = this;
        }

        public override void LoadContent()
        {
            Atlas = Content.LoadAtlas("Atlas/atlas.xml", "Atlas/atlas.png", true);
        }

        public override void Load()
        {
            RegisterAndLoad();
        }

        public override void Unload()
        {
            if (TFGame.Instance.Scene is Level && ServiceCollections.ResolveNetplayManager().IsServerMode())
            {
                ServiceCollections.ResolveReplayService().Export();
            }

            TF.EX.Domain.ServiceCollections.ServiceProvider.UnloadPatchs();
        }

        private void RegisterAndLoad()
        {
            typeof(ModExports).ModInterop();

            TF.EX.Domain.ServiceCollections.RegisterServices();
            TF.EX.Domain.ServiceCollections.ServiceCollection.RegisterPatchs();
            TF.EX.Domain.ServiceCollections.ServiceCollection.AddAPIManager();
            TF.EX.Domain.ServiceCollections.Build();
            TF.EX.Domain.ServiceCollections.ServiceProvider.LoadPatchs();
        }
    }
}
