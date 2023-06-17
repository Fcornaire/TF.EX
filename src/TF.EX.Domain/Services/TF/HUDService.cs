using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State.HUD;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    internal class HUDService : IHUDService
    {
        private IGameContext _gameContext;

        public HUDService(IGameContext gameContext)
        {
            _gameContext = gameContext;
        }

        public HUD Get()
        {
            return _gameContext.GetHUDState();
        }

        public void Update(HUD toLoad)
        {
            _gameContext.UpdateHUDState(toLoad);
        }
    }
}
