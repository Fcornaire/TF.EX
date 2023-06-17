using TF.EX.Domain.Models.State.HUD;

namespace TF.EX.Domain.Ports.TF
{
    public interface IHUDService
    {
        HUD Get();

        void Update(HUD toLoad);
    }
}
