using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs
{
    public class OrbLogicPatch : IHookable
    {
        private readonly IRngService _rngService;

        public OrbLogicPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

        public void Load()
        {
            On.TowerFall.OrbLogic.Update += OrbLogic_Update;
            On.TowerFall.OrbLogic.DoTimeOrb += OrbLogic_DoTimeOrb;
            On.TowerFall.OrbLogic.DoDarkOrb += OrbLogic_DoDarkOrb;
            On.TowerFall.OrbLogic.DoSpaceOrb += OrbLogic_DoSpaceOrb;
        }

        public void Unload()
        {
            On.TowerFall.OrbLogic.Update -= OrbLogic_Update;
            On.TowerFall.OrbLogic.DoTimeOrb -= OrbLogic_DoTimeOrb;
            On.TowerFall.OrbLogic.DoDarkOrb -= OrbLogic_DoDarkOrb;
            On.TowerFall.OrbLogic.DoSpaceOrb -= OrbLogic_DoSpaceOrb;
        }

        /// <summary>
        /// Reworked since the original use Random.Choose which is generic and not patchable
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void OrbLogic_DoSpaceOrb(On.TowerFall.OrbLogic.orig_DoSpaceOrb orig, TowerFall.OrbLogic self)
        {
            if (!self.Level.Ending)
            {
                _rngService.Get().ResetRandom();

                Vector2 start = TFGame.Instance.Screen.Offset;
                Vector2 end = start;
                end.X = Monocle.Calc.Snap(end.X, 320f, self.Level.Session.MatchSettings.Variants.OffsetWorld ? 160 : 0);
                end.Y = Monocle.Calc.Snap(end.Y, 240f, self.Level.Session.MatchSettings.Variants.OffsetWorld ? 120 : 0);
                end += Monocle.Calc.Random.Choose(new Vector2(-320f, 0f), new Vector2(320f, 0f), new Vector2(0f, -240f), new Vector2(0f, 240f));

                _rngService.AddGen(Domain.Models.State.RngGenType.Integer);

                var dynOrbLogic = DynamicData.For(self);
                var spaceTween = dynOrbLogic.Get<Tween>("spaceTween");

                spaceTween = Tween.Create(Tween.TweenMode.Persist, Ease.CubeInOut, 360, start: true);
                spaceTween.OnUpdate = delegate (Tween t)
                {
                    TFGame.Instance.Screen.Offset = Vector2.Lerp(start, end, t.Eased);
                };
                spaceTween.OnComplete = delegate
                {
                    spaceTween = null;
                };
                spaceTween.Start();

                var dynSpaceTween = DynamicData.For(spaceTween);
                dynSpaceTween.Add("ScreenOffsetStart", start);
                dynSpaceTween.Add("ScreenOffsetEnd", end);

                dynOrbLogic.Set("spaceTween", spaceTween);
            }
        }

        private void OrbLogic_DoDarkOrb(On.TowerFall.OrbLogic.orig_DoDarkOrb orig, TowerFall.OrbLogic self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();
        }

        private void OrbLogic_DoTimeOrb(On.TowerFall.OrbLogic.orig_DoTimeOrb orig, TowerFall.OrbLogic self, bool delay)
        {
            CalcPatch.RegisterRng();
            orig(self, delay);
            CalcPatch.UnregisterRng();
        }

        private void OrbLogic_Update(On.TowerFall.OrbLogic.orig_Update orig, TowerFall.OrbLogic self)
        {
            CalcPatch.RegisterRng();
            orig(self);
            CalcPatch.UnregisterRng();
        }
    }
}
