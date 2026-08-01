using FortRise;
using Microsoft.Extensions.Logging;
using MonoMod.ModInterop;
using TF.State.Core.Api;
using TF.State.Domain;

namespace TF.State.Core
{
    internal class TFStateModule : Mod
    {
        public static TFStateModule Instance { get; private set; }

        private readonly TfStateApi _api = new TfStateApi();

        public TFStateModule(IModContent content, IModuleContext context, ILogger logger)
            : base(content, context, logger)
        {
            Instance = this;

            ServiceCollections.SetLogger(logger);

            typeof(Api.ModExports).ModInterop();

            context.Harmony.PatchAll(typeof(TF.State.Patchs.Calc.CalcPatch).Assembly);
            context.Harmony.PatchAll(typeof(Patchs.StateFrameDriverPatch).Assembly);

            OnUnload = _ =>
            {
                _api.SetFrameDriver(null);
            };
        }

        public override object GetApi() => _api;
    }
}
