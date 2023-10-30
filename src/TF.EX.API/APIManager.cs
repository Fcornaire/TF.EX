using FortRise;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TF.EX.Domain.Ports;

namespace TF.EX.API
{
    public class APIManager : IAPIManager
    {
        private ConcurrentDictionary<string, IStateEvents> stateEvents = new ConcurrentDictionary<string, IStateEvents>();
        private ConcurrentBag<string> safeModules = new ConcurrentBag<string>();

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

        public void MarkModuleAsSafe(FortModule module)
        {
            if (safeModules.Contains(module.ID))
            {
                FortRise.Logger.Log("[TF.EX.API] Module already marked as safe " + module.ID, FortRise.Logger.LogLevel.Info);
                return;
            }

            safeModules.Add(module.ID);
        }

        public bool IsModuleSafe(string id)
        {
            return safeModules.Contains(id);
        }
    }
}
