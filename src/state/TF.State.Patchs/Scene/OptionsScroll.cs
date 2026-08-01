using HarmonyLib;
using TowerFall;

namespace TF.State.Patchs.Scene
{
    [HarmonyPatch(typeof(MainMenu))]
    internal static class OptionsScrollPatch
    {
        //FortRise fix, need a PR there
        [HarmonyPostfix]
        [HarmonyPatch("InitOptions")]
        public static void MainMenu_InitOptions(MainMenu __instance, List<OptionsButton> buttons)
        {
            if (buttons == null || buttons.Count == 0)
            {
                return;
            }

            var lastY = buttons.Max(button => button.TweenTo.Y);

            __instance.MaxUICameraY = Math.Max(__instance.MaxUICameraY, Math.Max(0f, lastY - 120f));
        }
    }
}
