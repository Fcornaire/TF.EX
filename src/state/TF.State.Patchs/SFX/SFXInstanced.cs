using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;

using TF.State.Domain.Context;
namespace TF.State.Patchs.SFX
{
    [HarmonyPatch(typeof(SFXInstanced))]
    public class SFXInstancedPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SFXInstanced.Play))]
        public static bool SFXInstanced_Play(SFXInstanced __instance, float panX, float volume)
        {

            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            if (StateFlags.IsRestoring)
            {
                return true; //Ignore SFXs on the first frame of a rollback (Coroutines update might play a sound)
            }

            var dynSFX = DynamicData.For(__instance);

            if (__instance.Data != null && !(Audio.MasterVolume <= 0f))
            {
                var sfxService = ServiceCollections.ResolveSFXService();
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                volume *= Audio.MasterVolume;

                var sfxToPlay = new TF.State.Domain.Models.SFX
                {
                    Frame = (int)Monocle.Engine.Instance.Scene.FrameCounter,
                    Name = sfxService.GetSoundEffectName(__instance.Data),
                    Volume = volume,
                    Pan = Monocle.SFX.CalculatePan(panX),
                    Data = __instance.Data
                };

                sfxService.AddDesired(sfxToPlay);
            }

            return false;
        }
    }
}
