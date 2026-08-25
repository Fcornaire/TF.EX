using FortRise;
using MonoMod.ModInterop;
using TF.State.Domain;
using TF.State.Domain.Context;

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
        /// True while the rollback system is replaying frames it has already run once.
        ///
        /// <para>Untracked cosmetics must not advance during a replay: their Update would run several times
        /// per frame and they would animate utra fast, so there is a guard to prevent it</para>
        /// </summary>
        public static bool ShouldFreezeCosmetics()
        {
            return StateFlags.IsRollbackFrame || StateFlags.HasFramesToReSimulate;
        }

        /// <summary>
        /// Make <c>Monocle.Calc.Random</c> the tracked gameplay RNG until <see cref="UnregisterRng"/>
        /// </summary>
        public static void RegisterRng()
        {
            TF.State.Patchs.Calc.CalcPatch.RegisterRng();
        }

        /// <summary>
        /// End the bracket opened by <see cref="RegisterRng"/>.
        /// </summary>
        public static void UnregisterRng()
        {
            TF.State.Patchs.Calc.CalcPatch.UnregisterRng();
        }

    }
}
