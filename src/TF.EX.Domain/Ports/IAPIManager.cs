using FortRise;

namespace TF.EX.Domain.Ports
{
    public interface IAPIManager
    {
        void RegisterVariantStateEvents(Mod module, string name, IStateEvents events);
        Dictionary<string, string> GetStates();
        void LoadStates(Dictionary<string, string> state);
        void MarkModuleAsSafe(Mod module);
        bool IsModuleSafe(string id);
    }

    public interface IStateEvents
    {
        string OnSaveState();
        void OnLoadState(string toLoad);
    }
}
