using HarmonyLib;
using MonoMod.Utils;
using TF.Replay.Domain;
using TF.Replay.Domain.Extensions;
using TowerFall;

namespace TF.Replay.Patchs.PlayerInput
{
    internal static class MenuAutoConfirm
    {
        internal static bool Confirm(bool actual)
        {
            if (!StandalonePlayback.IsActive)
            {
                return actual;
            }

            if (TFGame.Instance.Scene is not Level level || level.Paused)
            {
                return actual;
            }

            return !AtMatchEnd(level);
        }

        internal static bool Start(bool actual)
        {
            if (!StandalonePlayback.IsActive)
            {
                return actual;
            }

            if (TFGame.Instance.Scene is not Level level || level.Paused)
            {
                return actual;
            }

            return actual && !AtMatchEnd(level);
        }

        private static bool AtMatchEnd(Level level)
        {
            var matchResults = level.Get<VersusMatchResults>();

            return matchResults != null && DynamicData.For(matchResults).Get<bool>("focused");
        }

        internal static bool SkipReplay(bool actual) => actual || StandalonePlayback.IsActive;

        internal static InputState Gameplay(InputState actual, TowerFall.PlayerInput device)
        {
            if (!StandalonePlayback.IsActive || TFGame.Instance.Scene is not Level)
            {
                return actual;
            }

            var seat = Array.IndexOf(TFGame.PlayerInputs, device);

            if (Takeover.IsLive && seat == Takeover.Seat)
            {
                return Takeover.IsCapturing ? actual : Takeover.LiveInput;
            }

            return PlaybackInputs.ForSeat(seat);
        }
    }

    [HarmonyPatch(typeof(XGamepadInput))]
    internal static class XGamepadInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirm")]
        public static void MenuConfirm(ref bool __result) => __result = MenuAutoConfirm.Confirm(__result);

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStart")]
        public static void MenuStart(ref bool __result) => __result = MenuAutoConfirm.Start(__result);

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuSkipReplay")]
        public static void MenuSkipReplay(ref bool __result) => __result = MenuAutoConfirm.SkipReplay(__result);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(XGamepadInput.GetState))]
        public static void GetState(ref InputState __result, XGamepadInput __instance)
            => __result = MenuAutoConfirm.Gameplay(__result, __instance);
    }

    [HarmonyPatch(typeof(KeyboardInput))]
    internal static class KeyboardInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("get_MenuConfirm")]
        public static void MenuConfirm(ref bool __result) => __result = MenuAutoConfirm.Confirm(__result);

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuStart")]
        public static void MenuStart(ref bool __result) => __result = MenuAutoConfirm.Start(__result);

        [HarmonyPostfix]
        [HarmonyPatch("get_MenuSkipReplay")]
        public static void MenuSkipReplay(ref bool __result) => __result = MenuAutoConfirm.SkipReplay(__result);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(KeyboardInput.GetState))]
        public static void GetState(ref InputState __result, KeyboardInput __instance)
            => __result = MenuAutoConfirm.Gameplay(__result, __instance);
    }
}
