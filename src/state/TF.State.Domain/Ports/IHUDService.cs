using TF.State.Domain.Models.Entity.HUD;

namespace TF.State.Domain.Ports
{
    public interface IHUDService
    {
        HUD Get();

        void Update(HUD toLoad);
    }
}
