using FortRise;
using MonoMod.ModInterop;
using TF.State.Domain;

namespace TF.State.Core.Api
{
    /// <summary>
    /// Mod interop with other mods.
    /// </summary>
    [ModExportName("TF.State.API")]
    public static class ModExports
    {
        /// <summary>
        /// Register custom SaveState/LoadState events for a variant.
        ///
        /// <para>Those are used by the rollback system to properly save/load variant custom properties</para>
        /// <para>State is an opaque byte[]: it is carried inside the rollback snapshot, so it must be
        /// deterministic and must not depend on anything outside the simulation.</para>
        /// </summary>
        public static void RegisterVariantStateEvents(Mod module, string name, Func<byte[]> OnGetState, Action<byte[]> OnLoadState)
        {
            ServiceCollections.ResolveAPIManager().RegisterStateEvents(module.Meta.Name, name, OnGetState, OnLoadState);
        }

        /// <summary>
        /// Stop receiving SaveState/LoadState events for a variant.
        /// </summary>
        public static void UnregisterVariantStateEvents(Mod module, string name)
        {
            ServiceCollections.ResolveAPIManager().UnregisterStateEvents(module.Meta.Name, name);
        }

        /// <summary>
        /// Mark a module as netplay safe.
        ///
        /// <para>This is only to prevent EX showing a warning when a mod is loaded, and to let the mod's
        /// pickups through the treasure spawner.</para>
        ///
        /// <para> It does not mean the mod is compatible and test should be done first. </para>
        /// </summary>
        public static void MarkModuleAsSafe(Mod module)
        {
            ServiceCollections.ResolveAPIManager().MarkModuleAsSafe(module.Meta.Name);
        }
    }
}
