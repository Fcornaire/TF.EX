namespace TF.State.Domain.Ports
{
    public interface IAPIManager
    {
        Dictionary<string, string> GetStates(TowerFall.MatchVariants variants);
        void LoadStates(Dictionary<string, string> state);

        bool HasStateEvents(string id);

        void RegisterStateEvents(string modName, string key, System.Func<byte[]> onSaveState, System.Action<byte[]> onLoadState, bool isVariant);
        void UnregisterStateEvents(string modName, string key);

        IEnumerable<EntityStateEvents> GetEntityStateEvents();
        void RegisterEntityStateEvents(string modName, string name, System.Func<Monocle.Entity, bool> owns, System.Func<Monocle.Entity, byte[]> onSaveState, System.Action<Monocle.Entity, byte[]> onLoadState, System.Func<Monocle.Entity> create);
        void UnregisterEntityStateEvents(string modName, string name);
    }

    /// <summary>
    /// A mod's entity type in the rollback state: each entity's bytes, and how to make one again (null: TF.State makes it, an arrow).
    /// </summary>
    public record EntityStateEvents(string Key, System.Func<Monocle.Entity, bool> Owns, System.Func<Monocle.Entity, byte[]> OnSaveState, System.Action<Monocle.Entity, byte[]> OnLoadState, System.Func<Monocle.Entity> Create);

    public interface IStateEvents
    {
        /// <summary>The variant this state belongs to (saved only while it is on), or null.</summary>
        string GetVariantId();
        string OnSaveState();
        void OnLoadState(string toLoad);
    }
}
