using MonoMod.ModInterop;

namespace TF.EX.API
{
    /// <summary>
    /// Mod interop with other mods.
    /// </summary>
    [ModExportName("TF.EX.API")]
    public static class ModExports
    {
        /// <summary>
        /// Determine if the game is currently playing online.
        ///
        /// <para>Might be useful i dont know</para>
        /// </summary>
        public static bool IsPlayingOnline()
        {
            return TF.EX.Domain.ServiceCollections.ResolveNetplayManager().IsInit();
        }
    }
}
