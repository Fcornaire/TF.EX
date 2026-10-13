using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(FortRise.ModIO))]
    public class ModIOPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(FortRise.ModIO.LoadXml), typeof(string))]
        public static void ModIO_LoadXml(ref string path)
        {
            if (StateFlags.IsCaptureActive && !OperatingSystem.IsWindows())
            {
                path = path.Replace('\\', '/');
            }
        }
    }
}
