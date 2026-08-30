using HarmonyLib;
using TF.InputDisplayer.Domain;
using TF.InputDisplayer.Domain.Models;
using TowerFall;

namespace TF.InputDisplayer.Patchs
{
    [HarmonyPatch(typeof(ReplayViewer))]
    internal static class ReplayViewerPatch
    {
        private static readonly AccessTools.FieldRef<ReplayViewer, ReplayData> Data = AccessTools.FieldRefAccess<ReplayViewer, ReplayData>("data");

        private static readonly AccessTools.FieldRef<ReplayViewer, int> FrameMarker = AccessTools.FieldRefAccess<ReplayViewer, int>("frameMarker");

        private static readonly AccessTools.FieldRef<ReplayViewer, InputRenderer[]> InputRenderers = AccessTools.FieldRefAccess<ReplayViewer, InputRenderer[]>("inputRenderers");

        private static readonly InputHistory _history = new InputHistory();

        private static bool _ready;

        internal static bool Active => DisplayOptions.Enabled && DisplayOptions.ShowInInstantReplay;

        internal static void Render(ReplayViewer viewer, bool outside)
        {
            if (!_ready || !Active)
            {
                return;
            }

            if (outside)
            {
                InputDisplay.RenderOutside(_history, FrameMarker(viewer));
            }
            else
            {
                InputDisplay.RenderInside(_history, FrameMarker(viewer));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("PostScreenRender")]
        public static void ReplayViewer_PostScreenRender(ReplayViewer __instance)
        {
            Render(__instance, outside: false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        public static void ReplayViewer_ctor(ReplayViewer __instance)
        {
            if (Active)
            {
                InputRenderers(__instance) = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Watch")]
        public static void ReplayViewer_Watch(ReplayViewer __instance)
        {
            _ready = false;

            if (!Active)
            {
                return;
            }

            var frames = Data(__instance)?.Frames;

            if (frames == null)
            {
                return;
            }

            _history.Begin(InputHistory.MaxSeats);

            for (int frame = 0; frame < frames.Length; frame++)
            {
                var input = frames[frame]?.Input;

                if (input == null)
                {
                    continue;
                }

                for (int seat = 0; seat < InputHistory.MaxSeats; seat++)
                {
                    var state = seat < input.Length ? input[seat] : default;
                    var packed = InputPacker.Pack(state.MoveX, state.MoveY, state.JumpCheck, state.ShootCheck, state.AltShootCheck, state.DodgeCheck,state.JumpPressed, state.ShootPressed, state.AltShootPressed, state.DodgePressed);
                    _history.Push(frame, seat, packed);
                }
            }

            _ready = _history.HasFrames;
        }
    }
}
