using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface ISessionService
    {
        Session GetSession();
        void SaveSession(Session session);

        Dictionary<int, double> GetGamePlayLayerActualDepthLookup();
        void SaveGamePlayLayerActualDepthLookup(Dictionary<int, double> toSave);
    }
}
