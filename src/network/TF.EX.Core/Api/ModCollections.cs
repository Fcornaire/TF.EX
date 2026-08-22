using System;
using System.Collections.Generic;
using System.Linq;
using FortRise;
using Microsoft.Extensions.Logging;
using TF.EX.Common.Interop;
using TF.EX.Domain.Interop;

namespace TF.EX.Core.Api
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
            var metadata = _context?.Interop?.LoadedMods
                ?.FirstOrDefault(mod => string.Equals(mod?.Metadata?.Name, modName, StringComparison.OrdinalIgnoreCase))
                ?.Metadata;

            return metadata?.Version.ToString();
        }

        public ITfStateApi ResolveState() => Resolve<ITfStateApi>(TfStateApiData.Name);

        public ITfReplayApi ResolveReplay() => Resolve<ITfReplayApi>(TfReplayApiData.Name);

        public IWiderSetModApi ResolveWiderSet() => Resolve<IWiderSetModApi>(WiderSetModApiData.Name);

        public IInputDisplayerApi ResolveInputDisplayer() => Resolve<IInputDisplayerApi>(InputDisplayerApiData.Name);

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
