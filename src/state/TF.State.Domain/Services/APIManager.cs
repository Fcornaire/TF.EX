using System.Collections.Concurrent;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    public class APIManager : IAPIManager
    {
        private ConcurrentDictionary<string, IStateEvents> stateEvents = new();
        private ConcurrentDictionary<string, EntityStateEvents> entityStateEvents = new();

        public Dictionary<string, string> GetStates(TowerFall.MatchVariants variants)
        {
            var state = new Dictionary<string, string>();

            foreach (var pair in stateEvents.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (variants.CustomVariants.TryGetValue(pair.Value.GetVariantId(), out var variant) && !variant.Value)
                {
                    continue;
                }

                var saved = pair.Value.OnSaveState();
                if (!string.IsNullOrEmpty(saved))
                {
                    state.Add(pair.Key, saved);
                }
            }

            return state;
        }

        public void LoadStates(Dictionary<string, string> state)
        {
            foreach (var pair in stateEvents)
            {
                if (state.TryGetValue(pair.Key, out var toLoad))
                {
                    pair.Value.OnLoadState(toLoad);
                }
            }
        }

        public bool HasStateEvents(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var separator = id.IndexOf('/');

            return separator > 0
                ? stateEvents.ContainsKey(id.Substring(0, separator) + "-" + id.Substring(separator + 1))
                : stateEvents.Keys.Any(key => key.StartsWith(id + "-", StringComparison.Ordinal));
        }

        public void RegisterStateEvents(string modName, string key, Func<byte[]> onSaveState, Action<byte[]> onLoadState)
        {
            var id = $"{modName}-{key}";
            stateEvents.TryAdd(id, new ByteStateEvents($"{modName}/{key}", onSaveState, onLoadState));
        }

        public void UnregisterStateEvents(string modName, string key)
        {
            stateEvents.TryRemove($"{modName}-{key}", out _);
        }

        public IEnumerable<EntityStateEvents> GetEntityStateEvents()
        {
            return entityStateEvents.Values.OrderBy(events => events.Key, StringComparer.Ordinal);
        }

        public void RegisterEntityStateEvents(string modName, string name, Func<Monocle.Entity, bool> owns, Func<Monocle.Entity, byte[]> onSaveState, Action<Monocle.Entity, byte[]> onLoadState, Func<Monocle.Entity> create)
        {
            var id = $"{modName}-{name}";
            entityStateEvents.TryAdd(id, new EntityStateEvents(id, owns, onSaveState, onLoadState, create));
        }

        public void UnregisterEntityStateEvents(string modName, string name)
        {
            entityStateEvents.TryRemove($"{modName}-{name}", out _);
        }

        private class ByteStateEvents(string variantId, Func<byte[]> onSave, Action<byte[]> onLoad) : IStateEvents
        {
            public string OnSaveState()
            {
                var bytes = onSave?.Invoke();
                return bytes == null ? string.Empty : Convert.ToBase64String(bytes);
            }

            public void OnLoadState(string toLoad)
            {
                if (onLoad == null)
                {
                    return;
                }

                onLoad(string.IsNullOrEmpty(toLoad) ? Array.Empty<byte>() : Convert.FromBase64String(toLoad));
            }

            public string GetVariantId()
            {
                return variantId;
            }
        }
    }
}
