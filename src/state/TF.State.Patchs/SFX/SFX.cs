using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain.Context;

namespace TF.State.Patchs.SFX
{
    [HarmonyPatch(typeof(Monocle.SFX))]
    public class SFXPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(string), typeof(bool)])]
        public static void SFX_ctor_string_bool(Monocle.SFX __instance, string filename)
        {
            var sfxService = TF.State.Domain.ServiceCollections.ResolveSFXService();

            sfxService.AddSoundEffect(__instance.Data, filename);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Play")]
        public static bool SFX_Play(Monocle.SFX __instance, float panX, float volume)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            if (!StateFlags.IsSfxCaptureActive || Monocle.Engine.Instance?.Scene is not TowerFall.Level)
            {
                return true;
            }

            if (StateFlags.IsRestoring)
            {
                return false; //Ignore SFXs on the first frame of a rollback (Coroutines update might play a sound)
            }

            var dynSFX = DynamicData.For(__instance);

            if (__instance.Data != null)
            {
                var sfxService = TF.State.Domain.ServiceCollections.ResolveSFXService();
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                var pan = Monocle.SFX.CalculatePan(panX);

                var sfxToPlay = new TF.State.Domain.Models.SFX
                {
                    Frame = (int)Monocle.Engine.Instance.Scene.FrameCounter,
                    Name = sfxService.GetSoundEffectName(__instance.Data),
                    Volume = volume,
                    Pan = pan,
                    ObeysMasterPitch = __instance.ObeysMasterPitch,
                    Data = __instance.Data
                };

                sfxService.AddDesired(sfxToPlay);
            }

            return false;
        }
    }

}
