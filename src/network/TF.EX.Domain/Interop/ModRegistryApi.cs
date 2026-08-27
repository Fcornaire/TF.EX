namespace TF.EX.Domain.Interop
{
    public static class ModRegistryApi
    {
        public static FortRise.IModRegistry Current { get; private set; }

        public static string ModName { get; private set; } = "";

        public static void Configure(FortRise.IModRegistry registry, string modName)
        {
            Current = registry;
            ModName = modName ?? "";
        }
    }
}
