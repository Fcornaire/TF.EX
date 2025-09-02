using HarmonyLib;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;

namespace TF.EX.Patchs.SFX
{
    [HarmonyPatch(typeof(SFXInstanced))]
    public class SFXInstancedPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SFXInstanced.Play))]
        public static bool SFXInstanced_Play(SFXInstanced __instance, float panX, float volume)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (!netplayManager.IsInit())
            {
                return true;
            }

            if (netplayManager.IsUpdating())
            {
                return true; //Ignore SFXs on the first frame of a rollback (Coroutines update might play a sound)
            }

            var dynSFX = DynamicData.For(__instance);

            if (__instance.Data != null && !(Audio.MasterVolume <= 0f))
            {
                var sfxService = ServiceCollections.ResolveSFXService();
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                volume *= Audio.MasterVolume;

                var sfxToPlay = new Domain.Models.State.SFX
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
