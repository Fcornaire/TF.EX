using FortRise;
using Microsoft.Extensions.Logging;
using TF.InputDisplayer.Domain;
using TF.InputDisplayer.Domain.Api;
using TF.InputDisplayer.Domain.Interop;

namespace TF.InputDisplayer.Core
{
    internal class InputDisplayerModule : Mod
    {
        private const string IconFolder = "Content/directions";

        private readonly InputDisplayerApi _api = new InputDisplayerApi();

        private IWiderSetModApi _widerSet;
        private bool _widerSetResolved;

        public InputDisplayerModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
        {
            OnInitialize = _ => Initialize(content, context, logger);
        }

        public override ModuleSettings CreateSettings() => new InputDisplayerSettings();

        public override object GetApi() => _api;

        private void Initialize(IModContent content, IModuleContext context, ILogger logger)
        {
            GetSettings<InputDisplayerSettings>()?.Apply();

            RegisterIcons(content, context, logger);

            ScreenBounds.WideOffset = () => WiderSet(context)?.UIXOffset ?? 0f;

            context.Harmony.PatchAll(typeof(Patchs.PatchesEntry).Assembly);
        }

        private void RegisterIcons(IModContent content, IModuleContext context, ILogger logger)
        {
            foreach (var name in InputIcons.Shipped)
            {
                if (!content.TryGetResource($"{IconFolder}/{name}.png", out var resource))
                {
                    logger.LogError("Missing input icon {name}", name);
                    continue;
                }

                var entry = context.Registry.Subtextures.RegisterTexture(name, resource, SubtextureAtlasDestination.MenuAtlas);

                InputIcons.Register(name, () => entry.Subtexture);
            }
        }

        private IWiderSetModApi WiderSet(IModuleContext context)
        {
            if (_widerSetResolved)
            {
                return _widerSet;
            }

            _widerSetResolved = true;

            try
            {
                _widerSet = context.Interop.GetApi<IWiderSetModApi>(ModData.WiderSetName);
            }
            catch (Exception e)
            {
                context.Logger.LogWarning("Could not resolve WiderSet: {error}", e.Message);
            }

            return _widerSet;
        }
    }
}
