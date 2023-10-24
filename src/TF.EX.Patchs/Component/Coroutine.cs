using MonoMod.Utils;
using System.Collections;
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
            On.Monocle.Coroutine.ctor_IEnumerator += Coroutine_ctor_IEnumerator;
        }

        public void Unload()
        {
            On.Monocle.Coroutine.Update -= Coroutine_Update;
            On.Monocle.Coroutine.ctor_IEnumerator -= Coroutine_ctor_IEnumerator;
        }

        private void Coroutine_ctor_IEnumerator(On.Monocle.Coroutine.orig_ctor_IEnumerator orig, Monocle.Coroutine self, IEnumerator functionCall)
        {
            orig(self, functionCall);

            var dynCoroutine = DynamicData.For(self);
            dynCoroutine.Set("NAME", functionCall.GetType().Name);
        }

        private void Coroutine_Update(On.Monocle.Coroutine.orig_Update orig, Monocle.Coroutine self)
        {
            if (self.Entity is TowerFall.Miasma)
            {
                var session = _sessionService.GetSession();

                if (session.Miasma.IsDissipating)
                {
                    session.Miasma.DissipateTimer += 1;
                }
                else
                {
                    session.Miasma.CoroutineTimer += 1;
                }
            }

            if (self.Entity is TowerFall.VersusStart)
            {
                var dynCoroutine = DynamicData.For(self);
                if (dynCoroutine.TryGet("NAME", out string name))
                {
                    if (name.Contains("SetupSequence"))
                    {
                        var hud = _hudService.Get();
                        hud.VersusStart.CoroutineState += 1;
                        _hudService.Update(hud);
                    }
                }
            }

            if (self.Entity is TowerFall.VersusRoundResults)
            {
                var hud = _hudService.Get();
                hud.VersusRoundResults.CoroutineState += 1;

                _hudService.Update(hud);
            }

            orig(self);
        }
    }
}
