using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    public class ArrowService : IArrowService
    {
        private readonly IGameContext _context;

        public ArrowService(IGameContext context)
        {
            _context = context;
        }

        public void AddStuckArrow(Vector2f arrowPosition, TowerFall.Platform platform)
        {
            _context.AddStuckArrow(arrowPosition, platform);
        }

        public TowerFall.Platform GetPlatformStuck(Vector2f arrowPosition)
        {
            var stucks = _context.GetPlatoformStuckArrows();

            if (stucks.ContainsKey(arrowPosition))
            {
                return stucks[arrowPosition];
            }

            return null;
        }
    }
}
