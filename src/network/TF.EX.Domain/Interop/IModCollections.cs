using TF.EX.Common.Interop;

namespace TF.EX.Domain.Interop
{
    public interface IModCollections
    {
        string GetVersion(string modName);

        ITfStateApi ResolveState();

        ITfReplayApi ResolveReplay();

        IWiderSetModApi ResolveWiderSet();
    }
}
