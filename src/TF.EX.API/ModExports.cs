using FortRise;
using MonoMod.ModInterop;
using System;
using TF.EX.Domain.Ports;

namespace TF.EX.API
{
    /// <summary>
    /// Mod interop with other mods.
    /// </summary>
    [ModExportName("TF.EX.API")]
    public static class ModExports
    {
        /// <summary>
        /// Register custom SaveState/LoadState events for a variant.
        /// 
        /// <para>Those are used by EX rollback system to properly save/load variant custom properties</para>
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="OnGetState"></param>
        /// <param name="OnLoadState"></param>
        public static void RegisterVariantStateEvents(FortModule module, string name, Func<string> OnGetState, Action<string> OnLoadState)
        {
            var events = new StateEvents(OnGetState, OnLoadState);
            TF.EX.Domain.ServiceCollections.ResolveAPIManager().RegisterVariantStateEvents(module, name, events);
        }

        /// <summary>
        /// Mark a module as EX safe.
        /// 
        /// <para>This is only to prevent EX showing a warning when a mod is loaded.</para>
        /// 
        /// <para> It does not mean the mod is compatible with EX and test should be done first. </para>
        /// </summary>
        /// <param name="module"></param>
        public static void MarkModuleAsSafe(FortModule module)
        {
            TF.EX.Domain.ServiceCollections.ResolveAPIManager().MarkModuleAsSafe(module);
        }

        /// <summary>
        /// Determine if the game is currently playing online.
        /// 
        /// <para>Might be useful if a mod want to trigger some action</para>
        /// </summary>
        /// <returns></returns>
        public static bool IsPlayingOnline()
        {
            return TF.EX.Domain.ServiceCollections.ResolveNetplayManager().IsInit();
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
