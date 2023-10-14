using FortRise;
using MonoMod.ModInterop;
using System;

namespace TF.EX.API
{
    [ModExportName("TF.EX.API")]
    public static class ModExports
    {
        public static void RegisterVariantStateEvents(FortModule module, string name, Func<string> OnGetState, Action<string> OnLoadState)
        {
            var events = new StateEvents(OnGetState, OnLoadState);
            TF.EX.Domain.ServiceCollections.ServiceProvider.ResolveAPIManager().RegisterVariantStateEvents(module, name, events);
        }
    }

    public class StateEvents : IStateEvents
    {
        private Func<string> OnGetStateHandler;
        private Action<string> OnLoadStateHandler;

        public StateEvents(Func<string> OnGetState, Action<string> OnLoadState)
        {
            this.OnGetStateHandler = OnGetState;
            this.OnLoadStateHandler = OnLoadState;
        }

        public string OnSaveState()
        {
            return OnGetStateHandler();
        }

        public void OnLoadState(string toLoad)
        {
            OnLoadStateHandler(toLoad);
        }
    }
}
