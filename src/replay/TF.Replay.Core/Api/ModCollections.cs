using FortRise;
using Microsoft.Extensions.Logging;
using TF.Replay.Domain.Interop;

namespace TF.Replay.Core.Api
{
    internal sealed class ModCollections : IModCollections
    {
        private readonly IModuleContext _context;
        private readonly ILogger _logger;

        private readonly Dictionary<string, object> _apis = new Dictionary<string, object>();

        public ModCollections(IModuleContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public string GetVersion(string modName)
        {
            var metadata = _context?.Interop?.LoadedMods?.FirstOrDefault(mod => string.Equals(mod?.Metadata?.Name, modName, StringComparison.OrdinalIgnoreCase))?.Metadata;

            return metadata?.Version.ToString();
        }

        public ITfStateApi ResolveState()
        {
            var reported = _apis.ContainsKey(TfStateApiData.Name);

            var api = Resolve<ITfStateApi>(TfStateApiData.Name);

            if (api == null && !reported)
            {
                _logger.LogError("TF.State is unavailable ?");
            }

            return api;
        }

        public IWiderSetModApi ResolveWiderSet() => Resolve<IWiderSetModApi>(ModData.WiderSetName);

        public IInputDisplayerApi ResolveInputDisplayer() => Resolve<IInputDisplayerApi>(InputDisplayerApiData.Name);

        public ITfExArcherSkinApi ResolveTfExArcherSkin() => Resolve<ITfExArcherSkinApi>(TfExApiData.Name);

        private T Resolve<T>(string modName) where T : class
        {
            if (_apis.TryGetValue(modName, out var cached))
            {
                return (T)cached;
            }

            T api = null;

            if (GetVersion(modName) != null)
            {
                try
                {
                    api = _context.Interop.GetApi<T>(modName);
                }
                catch (Exception e)
                {
                    _logger.LogError("Could not resolve the API of {mod}: {error}", modName, e);
                }
            }

            _apis[modName] = api;

            return api;
        }
    }
}
