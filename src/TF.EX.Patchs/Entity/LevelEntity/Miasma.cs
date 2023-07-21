using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    internal class MiasmaPatch : IHookable
    {
        private readonly ISessionService _sessionService;

        public MiasmaPatch(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public void Load()
        {
            On.TowerFall.Miasma.Dissipate += Miasma_Dissipate;
        }

        public void Unload()
        {
            On.TowerFall.Miasma.Dissipate -= Miasma_Dissipate;
        }

        private void Miasma_Dissipate(On.TowerFall.Miasma.orig_Dissipate orig, TowerFall.Miasma self)
        {
            var miasmaState = _sessionService.GetSession().Miasma;

            miasmaState.IsDissipating = true;
            miasmaState.Percent = self.Percent;
            miasmaState.SideWeight = self.SideWeight;

            orig(self);
        }
    }
}
