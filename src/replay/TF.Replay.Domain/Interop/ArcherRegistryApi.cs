namespace TF.Replay.Domain.Interop
{
    public static class ArcherRegistryApi
    {
        public static FortRise.IModArchers Current { get; private set; }

        public static void Configure(FortRise.IModArchers archers)
        {
            Current = archers;
        }
    }
}
