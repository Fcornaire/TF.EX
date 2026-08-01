using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Patchs.Calc;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(OrbLogic))]
    public class OrbLogicPatch
    {
        // Reworked since the original use Random.Choose which is generic and not patchable
        [HarmonyPrefix]
        [HarmonyPatch("DoSpaceOrb")]
        public static bool OrbLogic_DoSpaceOrb(OrbLogic __instance)
        {
            if (!__instance.Level.Ending && StateFlags.IsCaptureActive)
            {
                CalcPatch.RegisterRng();

                Vector2 start = TFGame.Instance.Screen.Offset;
                Vector2 end = start;
                end.X = Monocle.Calc.Snap(end.X, 320f, __instance.Level.Session.MatchSettings.Variants.OffsetWorld ? 160 : 0);
                end.Y = Monocle.Calc.Snap(end.Y, 240f, __instance.Level.Session.MatchSettings.Variants.OffsetWorld ? 120 : 0);
                end += Monocle.Calc.Random.Choose(new Vector2(-320f, 0f), new Vector2(320f, 0f), new Vector2(0f, -240f), new Vector2(0f, 240f));

                CalcPatch.UnregisterRng();

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
        [HarmonyPatch("DoOffsetWorldVariant")]
        public static bool OrbLogic_DoOffsetWorldVariant(OrbLogic __instance)
        {
            if (!__instance.Level.Ending && StateFlags.IsCaptureActive)
            {
                CalcPatch.RegisterRng();

                Vector2 start = TFGame.Instance.Screen.Offset;
                Vector2 end = Monocle.Calc.Random.Choose(new Vector2(160f, 120f), new Vector2(-160f, 120f), new Vector2(160f, -120f), new Vector2(-160f, -120f));

                CalcPatch.UnregisterRng();

                var dynOrbLogic = DynamicData.For(__instance);

                var spaceTween = Tween.Create(Tween.TweenMode.Persist, Ease.CubeInOut, 90, start: true);
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
