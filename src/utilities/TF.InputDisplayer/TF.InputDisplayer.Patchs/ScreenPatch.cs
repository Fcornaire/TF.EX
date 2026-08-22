using HarmonyLib;
using Monocle;
using TF.InputDisplayer.Domain;
using TowerFall;

namespace TF.InputDisplayer.Patchs
{
    [HarmonyPatch(typeof(Monocle.Screen))]
    internal static class ScreenPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("PadRender")]
        public static void Screen_PadRender()
        {
            if (!DisplayOptions.Enabled || Engine.Instance?.Scene is not Level level)
            {
                RenderQueue.Clear();
                return;
            }

            var viewer = level.ReplayViewer;

            if (viewer != null && viewer.Visible)
            {
                RenderQueue.Clear();
                ReplayViewerPatch.Render(viewer, outside: true);
                return;
            }

            if (RenderQueue.Consume(out var history, out var frame))
            {
                InputDisplay.RenderOutside(history, frame);
            }
        }
    }
}
