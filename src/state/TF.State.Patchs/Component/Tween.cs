using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Component
{
    [HarmonyPatch(typeof(Monocle.Tween))]
    internal class TweenPatch
    {
        private static readonly AccessTools.FieldRef<Stack<Monocle.Tween>> Cached =
            AccessTools.StaticFieldRefAccess<Stack<Monocle.Tween>>(AccessTools.Field(typeof(Monocle.Tween), "cached"));

        // Since the cached tween pool isn't rollbacked on netplay, let's just not use it to prevent weird leak
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Monocle.Tween.Removed))]
        public static void Tween_Removed(Monocle.Tween __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var cached = Cached();

            if (cached.Count > 0 && ReferenceEquals(cached.Peek(), __instance))
            {
                cached.Pop();
            }
        }
    }
}
