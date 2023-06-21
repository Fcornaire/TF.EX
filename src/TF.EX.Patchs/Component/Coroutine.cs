using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Component
{
    public class CoroutinePatch : IHookable
    {
        private ISessionService _sessionService;
        private IHUDService _hudService;

        public CoroutinePatch(ISessionService sessionService, IHUDService hudService)
        {
            _sessionService = sessionService;
            _hudService = hudService;
        }

        public void Load()
        {
            On.Monocle.Coroutine.Update += Coroutine_Update;
        }

        public void Unload()
        {
            On.Monocle.Coroutine.Update -= Coroutine_Update;
        }

        private void Coroutine_Update(On.Monocle.Coroutine.orig_Update orig, Monocle.Coroutine self)
        {
            if (self.Entity is TowerFall.Miasma)
            {
                var session = _sessionService.GetSession();
                session.Miasma.CoroutineTimer += 1;
                _sessionService.SaveSession(session);
            }

            if (self.Entity is TowerFall.VersusStart)
            {

                var hud = _hudService.Get();
                hud.VersusStart.CoroutineState += 1;
                _hudService.Update(hud);
            }

            orig(self);
        }
    }
}
