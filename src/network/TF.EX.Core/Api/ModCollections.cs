using FortRise;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TF.EX.Common.Interop;
using TF.EX.Domain.Interop;

namespace TF.EX.Core.Api
{
    internal sealed class ModCollections : IModCollections
    {
        private readonly IModuleContext _context;
        private readonly ILogger _logger;

        private static readonly string[] NetplaySafeMods =
        [
            "TF.EX",
            TfStateApiData.Name,
            TfReplayApiData.Name,
            InputDisplayerApiData.Name,
            WiderSetModApiData.Name,
        ];

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

        private static bool IsNetplaySafe(string name)
        {
            return name.Equals("FortRise", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("FortRise.", StringComparison.OrdinalIgnoreCase)
                || NetplaySafeMods.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetDesyncRiskMods()
        {
            var metas = (_context?.Interop?.LoadedMods ?? [])
                .Select(mod => mod?.Metadata)
                .Where(meta => !string.IsNullOrEmpty(meta?.Name))
                .ToList();

            var state = ResolveState();

            var exempt = metas
                .Where(meta => string.IsNullOrEmpty(meta.DLL)
                    || IsNetplaySafe(meta.Name)
                    || (state?.HasStateEvents(meta.Name) ?? false))
                .Select(meta => meta.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            //Handle libraries pulled by a safe mod
            bool addedNewExemption = true;

            while (addedNewExemption)
            {
                addedNewExemption = false;

                foreach (var meta in metas.Where(meta => exempt.Contains(meta.Name)))
                {
                    var dependencies = (meta.Dependencies ?? []).Concat(meta.OptionalDependencies ?? []);

                    foreach (var dependency in dependencies)
                    {
                        if (!string.IsNullOrEmpty(dependency?.Name) && exempt.Add(dependency.Name))
                        {
                            addedNewExemption = true;
                        }
                    }
                }
            }

            return metas
                .Select(meta => meta.Name)
                .Where(name => !exempt.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

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
