using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using TF.EX.Domain;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class MiasmaPatch : IHookable
    {
        private readonly ISessionService _sessionService;

        public MiasmaPatch(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public static void LoadState(TowerFall.Miasma miasma, TF.EX.Domain.Models.State.Miasma toLoad)
        {
            var dynMiama = DynamicData.For(miasma);

            miasma.Percent = toLoad.Percent;
            miasma.Collidable = toLoad.IsCollidable;
            var sine = dynMiama.Get<SineWave>("sine");
            sine.UpdateAttributes(toLoad.SineCounter);

            var tentaclesSine = dynMiama.Get<SineWave>("tentaclesSine");
            tentaclesSine.UpdateAttributes(toLoad.SineTentaclesWaveCounter);
        }

        public void Load()
        {
            On.TowerFall.Miasma.Update += Miasma_Update;
        }

        public void Unload()
        {
            On.TowerFall.Miasma.Update -= Miasma_Update;
        }

        private void Miasma_Update(On.TowerFall.Miasma.orig_Update orig, TowerFall.Miasma self)
        {
            if (_sessionService.GetSession().RoundEndCounter != 0 && !_sessionService.GetSession().IsEnding) //Ignore update on end for now
            {
                RemoveLastTween(self);
                AddCorrectTween(self);
            }
            else
            {
                self.Dissipate();
            }
            orig(self);
        }


        private void RemoveLastTween(Miasma self) //TODO: entity extension ?
        {
            foreach (var compo in self.Components.ToArray())
            {
                if (compo is Tween)
                {
                    Tween tween = (Tween)compo;
                    tween.RemoveSelf();
                    self.Components.Remove(tween);
                }
            }
        }

        private void AddCorrectTween(Miasma self)
        {
            Tween tween;
            var miasma = _sessionService.GetSession().Miasma;

            switch (miasma.State)
            {
                case TF.EX.Domain.Models.State.MiasmaState.Uninitialized: break;
                case TF.EX.Domain.Models.State.MiasmaState.Initialized:
                    tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 540, start: true);
                    tween.OnUpdate = delegate (Tween t)
                    {
                        self.Percent = MathHelper.Lerp(self.Percent, 0.33f, t.Eased);
                    };
                    self.Add(tween);
                    break;
                case TF.EX.Domain.Models.State.MiasmaState.Collidable:
                    tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 540, start: true);
                    tween.OnUpdate = delegate (Tween t)
                    {
                        self.Percent = MathHelper.Lerp(self.Percent, 0.33f, t.Eased);
                    };
                    self.Add(tween);
                    break;
                case TF.EX.Domain.Models.State.MiasmaState.Phase2:
                    tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 180, start: true);
                    tween.OnUpdate = delegate (Tween t)
                    {
                        self.Percent = MathHelper.Lerp(self.Percent, 0.15f, t.Eased);
                    };
                    self.Add(tween);
                    break;
                case TF.EX.Domain.Models.State.MiasmaState.NervesOfSteel:
                    tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 660, start: true);
                    var dynTween = DynamicData.For(tween);
                    dynTween.Set("FramesLeft", miasma.CoroutineTimer);
                    tween.OnUpdate = delegate (Tween t)
                    {
                        self.Percent = MathHelper.Lerp(self.Percent, 1.05f, t.Eased);
                    };
                    self.Add(tween);
                    break;
                case TF.EX.Domain.Models.State.MiasmaState.End:
                    tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 240, start: true);
                    tween.OnUpdate = delegate (Tween t)
                    {
                        self.Percent = MathHelper.Lerp(self.Percent, -0.1f, t.Eased);
                    };
                    self.Add(tween);
                    break;
                default:
                    break;
            }
        }

        public static IEnumerator GetCorrectSequence(TowerFall.Miasma miasma)
        {
            var dynMiasma = DynamicData.For(miasma);


            var methodInfo = typeof(TowerFall.Miasma).GetMethod("Sequence", BindingFlags.NonPublic | BindingFlags.Instance);
            //var sequence = (IEnumerator)methodInfo.Invoke(miasma, new object[] { });

            var sequenceDelegate = (Func<IEnumerator>)Delegate.CreateDelegate(typeof(Func<IEnumerator>), miasma, methodInfo);
            //var sequence = sequenceDelegate();

            //var sequence = dynMiasma.Get<IEnumerator>("Sequence");



            var sessionService = ServiceCollections.ResolveSessionService();

            switch (sessionService.GetSession().Miasma.State)
            {
                case TF.EX.Domain.Models.State.MiasmaState.Uninitialized: return sequenceDelegate();
                case TF.EX.Domain.Models.State.MiasmaState.Initialized: return SequenceInitialized(sequenceDelegate());
                case TF.EX.Domain.Models.State.MiasmaState.Collidable: return SequenceCollidable(sequenceDelegate());
                case TF.EX.Domain.Models.State.MiasmaState.Phase2: return SequencePhase2(sequenceDelegate());
                case TF.EX.Domain.Models.State.MiasmaState.NervesOfSteel: return SequenceNervesOfSteel(sequenceDelegate());
                case TF.EX.Domain.Models.State.MiasmaState.End: return SequenceEnd(sequenceDelegate());
                default:
                    return null;
            }
        }

        private static IEnumerator SequenceInitialized(IEnumerator sequence)
        {
            sequence.MoveNext();
            return sequence;
        }

        private static IEnumerator SequenceCollidable(IEnumerator sequence)
        {
            sequence.MoveNext();
            sequence.MoveNext();
            return sequence;
        }

        private static IEnumerator SequencePhase2(IEnumerator sequence)
        {
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            return sequence;
        }

        private static IEnumerator SequenceNervesOfSteel(IEnumerator sequence)
        {
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            return sequence;
        }

        private static IEnumerator SequenceEnd(IEnumerator sequence)
        {
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            sequence.MoveNext();
            return sequence;
        }
    }
}
