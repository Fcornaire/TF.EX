namespace TF.State.Domain.Ports
{
    public interface IAPIManager
    {
        Dictionary<string, string> GetStates(TowerFall.MatchVariants variants);
        void LoadStates(Dictionary<string, string> state);

        bool HasStateEvents(string id);

        void RegisterStateEvents(string modName, string key, System.Func<byte[]> onSaveState, System.Action<byte[]> onLoadState);
        void UnregisterStateEvents(string modName, string key);
    }

    public interface IStateEvents
    {
        string GetVariantId();
        string OnSaveState();
        void OnLoadState(string toLoad);
    }
}
