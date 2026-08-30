using FortRise;
using Microsoft.Extensions.Logging;
using TF.Replay.Domain.Api;
using TF.Replay.Domain.Interop;
using TF.Replay.Domain.Ports;
using TF.Replay.Domain;
using TF.Replay.Domain.Services;

namespace TF.Replay.Core
{
    internal class TFReplayModule : Mod
    {
        public static TFReplayModule Instance { get; private set; }

        private readonly TfReplayApi _api;
        private readonly IReplayService _replayService;
        private readonly IModCollections _modCollections;

        public TFReplayModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
        {
            Instance = this;

            ArcherRegistryApi.Configure(context.Registry.Archers);

            _modCollections = new Api.ModCollections(context, logger);

            _replayService = new ReplayService(() => _modCollections, logger);
            _api = new TfReplayApi(_replayService, () => _modCollections);

            ServiceCollections.Register(logger, _replayService, _api, () => _modCollections);

            OnInitialize = _ => Initialize(context, logger);
            OnUnload = _ =>
            {
                if (_replayService?.IsRecording == true)
                {
                    _replayService.Export();
                }

                if (StandaloneRecorder.IsActive || StandalonePlayback.IsActive)
                {
                    var state = ServiceCollections.ResolveStateApi();
                    if (state?.GetFrameDriver() == "TF.Replay")
                    {
                        state.SetFrameDriver(null);
                        state.SetDriverFlags(0, false, false, false, false, 0);
                    }

                    StandaloneRecorder.Reset();
                }

                _api?.SetRecordDriver(null);
            };
        }

        private void Initialize(IModuleContext context, ILogger logger)
        {
            LegacyReplayMigration.Run(logger);

            GetSettings<ReplaySettings>()?.Apply();

            new TFReplayCommands(_api).Register(context);

            context.Harmony.PatchAll(typeof(TFReplayModule).Assembly);
            context.Harmony.PatchAll(typeof(TF.Replay.Patchs.Scene.MainMenuPatch).Assembly);
        }

        public override ModuleSettings CreateSettings() => new ReplaySettings();

        public override object GetApi() => _api;

        internal static IReplayService Service => Instance?._replayService;

        internal static TfReplayApi Api => Instance?._api;
    }
}
