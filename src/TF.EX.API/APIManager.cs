using FortRise;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TF.EX.API
{
    public interface IStateEvents
    {
        string OnSaveState();
        void OnLoadState(string toLoad);
    }

    public interface IAPIManager
    {
        void RegisterVariantStateEvents(FortModule module, string name, IStateEvents events);
        Dictionary<string, string> GetStates();
        void LoadStates(Dictionary<string, string> state);
    }

    public class APIManager : IAPIManager
    {
        private ConcurrentDictionary<string, IStateEvents> stateEvents = new ConcurrentDictionary<string, IStateEvents>();

        public void RegisterVariantStateEvents(FortModule module, string name, IStateEvents events)
        {
            var id = $"{module.ID}-{name}";

            if (stateEvents.ContainsKey(id))
            {
                FortRise.Logger.Log("[TF.EX.API] State Events already registered for " + id, FortRise.Logger.LogLevel.Info);
                return;
            }

            if (!stateEvents.TryAdd(id, events))
            {
                FortRise.Logger.Log("[TF.EX.API] Failed to register State Events for " + id, FortRise.Logger.LogLevel.Info);
                return;
            }

            FortRise.Logger.Log("[TF.EX.API] State Events registered for " + id, FortRise.Logger.LogLevel.Info);
        }

        public Dictionary<string, string> GetStates()
        {
            var state = new Dictionary<string, string>();

            foreach (var pair in stateEvents)
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
    }
}
