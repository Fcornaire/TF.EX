using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Layer
{
    [HarmonyPatch(typeof(ReplayViewer))]
    public static class InstantReplayViewerPatch
    {
        private static readonly AccessTools.FieldRef<ReplayViewer, ReplayData> Data = AccessTools.FieldRefAccess<ReplayViewer, ReplayData>("data");
        private static readonly AccessTools.FieldRef<ReplayViewer, Action> OnComplete = AccessTools.FieldRefAccess<ReplayViewer, Action>("onComplete");
        private static readonly AccessTools.FieldRef<ReplayViewer, Coroutine> Routine = AccessTools.FieldRefAccess<ReplayViewer, Coroutine>("coroutine");
        private static readonly AccessTools.FieldRef<ReplayViewer, int> FrameMarker = AccessTools.FieldRefAccess<ReplayViewer, int>("frameMarker");
        private static readonly Func<ReplayViewer, IEnumerator> Rewind = AccessTools.MethodDelegate<Func<ReplayViewer, IEnumerator>>(AccessTools.Method(typeof(ReplayViewer), "Rewind"));
        private static readonly Action<ReplayViewer, int> SetFrame = AccessTools.MethodDelegate<Action<ReplayViewer, int>>(AccessTools.Method(typeof(ReplayViewer), "SetFrame"));

        private static Vector2 _offset;
        private static Vector2 _offsetAdd;
        private static bool _canSkip;
        private static bool _driving;

        internal static bool IsViewerUpdating { get; private set; }

        public static bool Start(ReplayData data)
        {
            if (Engine.Instance?.Scene is not Level level || data?.Frames == null || data.Frames.Length == 0)
            {
                return false;
            }

            var viewer = level.ReplayViewer;
            viewer.Active = viewer.Visible = true;
            Data(viewer) = data;
            SetFrame(viewer, data.Frames.Length - 1);
            OnComplete(viewer) = null;
            Routine(viewer) = new Coroutine(Rewind(viewer));
            Audio.Stop();

            return true;
        }

        public static bool IsPlaying() => Engine.Instance?.Scene is Level level && level.ReplayViewer.Visible;

        internal static bool TryGetReplayOffset(out Vector2 offset, out Vector2 offsetAdd)
        {
            offset = Vector2.Zero;
            offsetAdd = Vector2.Zero;

            if (!InstantReplay.Enabled || Engine.Instance?.Scene is not Level level || !level.ReplayViewer.Visible)
            {
                return false;
            }

            var frames = Data(level.ReplayViewer)?.Frames;
            var marker = FrameMarker(level.ReplayViewer);

            if (frames == null || marker < 0 || marker >= frames.Length)
            {
                return false;
            }

            offset = frames[marker].ScreenOffset;
            offsetAdd = frames[marker].ScreenOffsetAdd;

            return true;
        }

        public static void Tick()
        {
            if (Engine.Instance?.Scene is not Level level || !level.ReplayViewer.Visible)
            {
                return;
            }

            _driving = true;
            StateFlags.IsReplayViewing = true;

            try
            {
                level.ReplayViewer.Update();
            }
            finally
            {
                _driving = false;
                StateFlags.IsReplayViewing = false;
            }
        }

        public static void Stop()
        {
            if (Engine.Instance?.Scene is not Level level || !level.ReplayViewer.Visible)
            {
                return;
            }

            var viewer = level.ReplayViewer;
            viewer.Active = viewer.Visible = false;
            Routine(viewer) = null;
            ScreenEffects.Reset();
            Audio.Stop();
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool ReplayViewer_Update_Prefix()
        {
            if (!InstantReplay.Enabled)
            {
                return true;
            }

            if (!_driving)
            {
                return false;
            }

            _offset = Engine.Instance.Screen.Offset;
            _offsetAdd = Engine.Instance.Screen.OffsetAdd;
            IsViewerUpdating = true;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void ReplayViewer_Update_Postfix(bool __runOriginal)
        {
            if (!InstantReplay.Enabled || !__runOriginal)
            {
                return;
            }

            IsViewerUpdating = false;
            Engine.Instance.Screen.Offset = _offset;
            Engine.Instance.Screen.OffsetAdd = _offsetAdd;
        }

        [HarmonyPrefix]
        [HarmonyPatch("PostScreenRender")]
        public static void ReplayViewer_PostScreenRender_Prefix()
        {
            if (!InstantReplay.Enabled)
            {
                return;
            }

            _canSkip = SaveData.Instance.Options.CanSkipReplays;
            SaveData.Instance.Options.CanSkipReplays = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("PostScreenRender")]
        public static void ReplayViewer_PostScreenRender_Postfix()
        {
            if (!InstantReplay.Enabled)
            {
                return;
            }

            SaveData.Instance.Options.CanSkipReplays = _canSkip;
        }
    }

    [HarmonyPatch(typeof(Monocle.Screen))]
    internal static class InstantReplayScreenPatch
    {
        private static Vector2 _liveOffset;
        private static Vector2 _liveOffsetAdd;
        private static bool _isSwapped;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Screen.Render))]
        public static void Screen_Render_Prefix(Monocle.Screen __instance)
        {
            _isSwapped = InstantReplayViewerPatch.TryGetReplayOffset(out var offset, out var offsetAdd);

            if (!_isSwapped)
            {
                return;
            }

            _liveOffset = __instance.Offset;
            _liveOffsetAdd = __instance.OffsetAdd;
            __instance.Offset = offset;
            __instance.OffsetAdd = offsetAdd;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Monocle.Screen.Render))]
        public static void Screen_Render_Postfix(Monocle.Screen __instance)
        {
            if (!_isSwapped)
            {
                return;
            }

            __instance.Offset = _liveOffset;
            __instance.OffsetAdd = _liveOffsetAdd;
            _isSwapped = false;
        }
    }

    [HarmonyPatch(typeof(MenuInput))]
    internal static class InstantReplayMenuInputPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MenuInput.ReplaySkip), MethodType.Getter)]
        public static void MenuInput_ReplaySkip(ref bool __result)
        {
            if (InstantReplayViewerPatch.IsViewerUpdating)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MenuInput.AltCheck), MethodType.Getter)]
        public static void MenuInput_AltCheck(ref bool __result)
        {
            if (InstantReplayViewerPatch.IsViewerUpdating)
            {
                __result = false;
            }
        }
    }
}
