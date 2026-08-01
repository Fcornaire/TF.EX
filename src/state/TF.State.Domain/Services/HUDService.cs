using TF.State.Domain.Context;
using TF.State.Domain.Models.Entity.HUD;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    internal class HUDService : IHUDService
    {
        private IStateContext _stateContext;

        public HUDService(IStateContext gameContext)
        {
            _stateContext = gameContext;
        }

        public HUD Get()
        {
            return _stateContext.GetHUDState();
        }

        public void Update(HUD toLoad)
        {
            _stateContext.UpdateHUDState(toLoad);
        }
    }
}
