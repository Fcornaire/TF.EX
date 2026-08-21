using HarmonyLib;
using System.Reflection;
using TF.State.Domain.Logging;
using TF.State.Domain;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(Monocle.Audio))]
    internal class MonoclePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Stop")]
        public static bool Audio_Stop()
        {
            if (!TF.State.Domain.Context.StateFlags.IsCaptureActive)
            {
                return true;
            }

            try
            {
                var field = typeof(Monocle.Audio).GetField("pitchList", BindingFlags.NonPublic | BindingFlags.Static);
                var pitchList = (List<Monocle.SFX>)field.GetValue(null);

                pitchList.ToList().ForEach(pitch =>
                {
                    pitch.Stop();
                });
            }
            catch (Exception e)
            {
                var logger = ServiceCollections.ResolveLogger();
                logger.LogError<MonoclePatch>("Error stopping audio", e);
            }

            return false;
        }
    }
}
