using HarmonyLib;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Arrow))]
    public class ArrowPatch
    {
        //FieldRef here because its cheaper than allocating with DynamicData every Render
        private static readonly AccessTools.FieldRef<Arrow, Monocle.Image[]> ArrowGraphics = AccessTools.FieldRefAccess<Arrow, Monocle.Image[]>("Graphics");

        //TODO: Properly track arrow decay to remove this
        [HarmonyPrefix]
        [HarmonyPatch("EnforceLimit")]
        public static bool Arrow_EnforceLimit()
        {
            return !StateFlags.IsCaptureActive;
        }

        //TODO: remove this when a test without this is done
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Arrow.Removed))]
        public static void Arrow_Removed()
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            TowerFall.Arrow.FlushCache();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoWrapRender")]
        public static void Arrow_DoWrapRender(Arrow __instance, out float[] __state)
        {
            __state = null;

            if (StateFlags.IsTestMode || StateFlags.IsReplayMode)
            {
                __instance.DebugRender();
            }

            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var graphics = ArrowGraphics(__instance);
            __state = new float[graphics.Length];

            for (int i = 0; i < graphics.Length; i++)
            {
                __state[i] = graphics[i].Rotation;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoWrapRender")]
        public static void Arrow_DoWrapRender_Postfix(Arrow __instance, float[] __state)
        {
            if (__state == null)
            {
                return;
            }

            var graphics = ArrowGraphics(__instance);

            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].Rotation = __state[i];
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Drop")]
        public static void Arrow_Drop_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Drop")]
        public static void Arrow_Drop_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Init", [typeof(TowerFall.LevelEntity), typeof(Microsoft.Xna.Framework.Vector2), typeof(float)])]
        public static void Arrow_Init_Postfix(Arrow __instance)
        {
            MonoMod.Utils.DynamicData.For(__instance).Set("counter", Microsoft.Xna.Framework.Vector2.Zero);
        }

        [HarmonyPrefix]
        [HarmonyPatch("EnterFallMode")]
        public static void Arrow_EnterFallMode_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnterFallMode")]
        public static void Arrow_EnterFallMode_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }
    }
}
