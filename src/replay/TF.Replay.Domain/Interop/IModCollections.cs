namespace TF.Replay.Domain.Interop
{
    public interface IModCollections
    {
        string GetVersion(string modName);

        ITfStateApi ResolveState();

        IWiderSetModApi ResolveWiderSet();

        IInputDisplayerApi ResolveInputDisplayer();
    }

    public interface IWiderSetModApi
    {
        bool IsWide { get; set; }

        float UIXOffset { get; }
    }

    public static class ModData
    {
        public const string VersionKey = "Version";

        public const string StateEventsKey = "StateEvents";

        public const string UnstatedVariantsKey = "UnstatedVariants";

        public const string BannedVariantsKey = "BannedVariants";

        public const string WiderSetName = "Teuria.WiderSet";

        public const string IsWideKey = "IsWide";
    }
}
