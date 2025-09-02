using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Patchs.Calc;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(OrbLogic))]
    public class OrbLogicPatch
    {
        //TODO: Try with harmony generic patch
        /// <summary>
        /// Reworked since the original use Random.Choose which is generic and not patchable
        /// </summary>
        /// <param name="__instance"></param>

        [HarmonyPrefix]
        [HarmonyPatch("DoSpaceOrb")]
        public static bool OrbLogic_DoSpaceOrb(OrbLogic __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!__instance.Level.Ending && netplayManager.IsInit())
            {
                var rngService = ServiceCollections.ResolveRngService();
                rngService.Get().ResetRandom(ref Monocle.Calc.Random);

                Vector2 start = TFGame.Instance.Screen.Offset;
                Vector2 end = start;
                end.X = Monocle.Calc.Snap(end.X, 320f, __instance.Level.Session.MatchSettings.Variants.OffsetWorld ? 160 : 0);
                end.Y = Monocle.Calc.Snap(end.Y, 240f, __instance.Level.Session.MatchSettings.Variants.OffsetWorld ? 120 : 0);
                end += Monocle.Calc.Random.Choose(new Vector2(-320f, 0f), new Vector2(320f, 0f), new Vector2(0f, -240f), new Vector2(0f, 240f));

                rngService.AddGen(Domain.Models.State.RngGenType.Integer);

                var dynOrbLogic = DynamicData.For(__instance);
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

                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoDarkOrb")]
        public static void OrbLogic_DoDarkOrb_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoDarkOrb")]
        public static void OrbLogic_DoDarkOrb_Postfix()
        {
            CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoTimeOrb")]
        public static void OrbLogic_DoTimeOrb_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoTimeOrb")]
        public static void OrbLogic_DoTimeOrb_Postfix()
        {
            CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void OrbLogic_Update_Prefix()
        {
            CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void OrbLogic_Update_Postfix()
        {
            CalcPatch.UnregisterRng();
        }
    }
}
