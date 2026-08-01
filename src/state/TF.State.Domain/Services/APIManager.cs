using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    public class APIManager : IAPIManager
    {
        private ConcurrentDictionary<string, IStateEvents> stateEvents = new ConcurrentDictionary<string, IStateEvents>();
        private ConcurrentBag<string> safeModules = new ConcurrentBag<string>();

        public Dictionary<string, string> GetStates()
        {
            var state = new Dictionary<string, string>();

            foreach (var pair in stateEvents.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                state.Add(pair.Key, pair.Value.OnSaveState());
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

        public bool IsModuleSafe(string id)
        {
            return safeModules.Any((mod) => mod == id);
        }

        public bool HasStateEvents(string modName)
        {
            return !string.IsNullOrEmpty(modName) && stateEvents.Keys.Any(id => id.StartsWith(modName + "-", StringComparison.Ordinal));
        }

        public void RegisterStateEvents(string modName, string key, Func<byte[]> onSaveState, Action<byte[]> onLoadState)
        {
            var id = $"{modName}-{key}";
            stateEvents.TryAdd(id, new ByteStateEvents(onSaveState, onLoadState));
        }

        public void UnregisterStateEvents(string modName, string key)
        {
            stateEvents.TryRemove($"{modName}-{key}", out _);
        }

        public void MarkModuleAsSafe(string modName)
        {
            if (safeModules.Any((mod) => mod == modName))
            {
                return;
            }

            safeModules.Add(modName);
        }

        private sealed class ByteStateEvents : IStateEvents
        {
            private readonly Func<byte[]> _onSave;
            private readonly Action<byte[]> _onLoad;

            public ByteStateEvents(Func<byte[]> onSave, Action<byte[]> onLoad)
            {
                _onSave = onSave;
                _onLoad = onLoad;
            }

            public string OnSaveState()
            {
                var bytes = _onSave?.Invoke();
                return bytes == null ? string.Empty : Convert.ToBase64String(bytes);
            }

            public void OnLoadState(string toLoad)
            {
                if (_onLoad == null)
                {
                    return;
                }

                _onLoad(string.IsNullOrEmpty(toLoad) ? Array.Empty<byte>() : Convert.FromBase64String(toLoad));
            }
        }
    }
}
