using Monocle;
using MonoMod.Utils;
using System.Collections;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Entity.LevelEntity;
using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs.Component
{
    public class CoroutinePatch : IHookable
    {
        private ISessionService _sessionService;
        private INetplayManager _netplayManager;
        private IHUDService _hudService;

        public CoroutinePatch(ISessionService sessionService, INetplayManager netplayManager, IHUDService hudService)
        {
            _sessionService = sessionService;
            _netplayManager = netplayManager;
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
            var dynCoroutine = DynamicData.For(self);
            var enumerators = dynCoroutine.Get<Stack<IEnumerator>>("enumerators");

            IEnumerator enumerator = enumerators.Peek();
            var className = enumerator.GetClassName();

            if (className == typeof(TowerFall.Miasma).Name)
            {
                if (_netplayManager.HaveFramesToReSimulate())
                {
                    LoadState(dynCoroutine, enumerators, _sessionService.GetSession());
                }

                var currentTimer = (int)dynCoroutine.Get<Counter>("waitTimer").Value;

                orig(self);

                UpdateMiasmaState(dynCoroutine, currentTimer);
                SaveState((int)dynCoroutine.Get<Counter>("waitTimer").Value);
            }
            else if (self.Entity is TowerFall.VersusStart)
            {

                var hud = _hudService.Get();
                hud.VersusStart.CoroutineState += 1;
                _hudService.Update(hud);


                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void SaveState(int waitTimer)
        {
            var session = _sessionService.GetSession();

            session.Miasma.CoroutineTimer = waitTimer;

            _sessionService.SaveSession(session);
        }

        private void LoadState(DynamicData dynCoroutine, Stack<IEnumerator> enumerators, Session session)
        {
            var waitCounter = DynamicData.For(dynCoroutine.Get<Counter>("waitTimer"));
            waitCounter.Set("counter", session.Miasma.CoroutineTimer);


            enumerators.Pop();
            var miasma = TowerFall.TFGame.Instance.Scene.GetMiasma();
            enumerators.Push(MiasmaPatch.GetCorrectSequence(miasma));
        }

        private void UpdateMiasmaState(DynamicData dynCoroutine, int currentTimer)
        {
            var session = _sessionService.GetSession();
            var waitTimer = dynCoroutine.Get<Counter>("waitTimer");

            if (currentTimer == 0 && currentTimer != (int)waitTimer.Value)
            {
                session.Miasma.State = GetNextMiasmaState((int)waitTimer.Value);

                _sessionService.SaveSession(session);
            }

            if (currentTimer == 0 && (int)waitTimer.Value == 0 && session.Miasma.State == MiasmaState.NervesOfSteel)
            {
                session.Miasma.State = MiasmaState.End;
                _sessionService.SaveSession(session);
            }
        }

        private MiasmaState GetNextMiasmaState(int waitTimer)
        {
            switch (waitTimer)
            {
                case 120:
                    return MiasmaState.Initialized;
                case 420:
                    return MiasmaState.Collidable;
                case 180:
                    return MiasmaState.Phase2;
                case 660:
                    return MiasmaState.NervesOfSteel;
                default:
                    return _sessionService.GetSession().Miasma.State;
            }
        }
    }
}
