using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface IArrowService
    {
        void AddStuckArrow(Vector2f arrowPosition, TowerFall.Platform platform);
        TowerFall.Platform GetPlatformStuck(Vector2f arrowPosition);
    }
}
