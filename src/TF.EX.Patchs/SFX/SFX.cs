using HarmonyLib;
using MonoMod.Utils;

namespace TF.EX.Patchs.SFX
{
    [HarmonyPatch(typeof(Monocle.SFX))]
    public class SFXPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(string), typeof(bool)])]
        public static void SFX_ctor_string_bool(Monocle.SFX __instance, string filename)
        {
            var sfxService = TF.EX.Domain.ServiceCollections.ResolveSFXService();

            sfxService.AddSoundEffect(__instance.Data, filename);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Play")]
        public static bool SFX_Play(Monocle.SFX __instance, float panX, float volume)
        {
            var netplayManager = TF.EX.Domain.ServiceCollections.ResolveNetplayManager();

            if (!netplayManager.IsInit())
            {
                return true;
            }

            if (netplayManager.IsUpdating())
            {
                return false; //Ignore SFXs on the first frame of a rollback (Coroutines update might play a sound)
            }

            var dynSFX = DynamicData.For(__instance);

            if (__instance.Data != null && Monocle.Audio.MasterVolume > 0f)
            {
                var sfxService = TF.EX.Domain.ServiceCollections.ResolveSFXService();
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                volume *= Monocle.Audio.MasterVolume;
                var pitch = __instance.ObeysMasterPitch ? Monocle.Audio.MasterPitch : 0f;
                var pan = Monocle.SFX.CalculatePan(panX);

                var sfxToPlay = new Domain.Models.State.SFX
                {
                    Frame = (int)Monocle.Engine.Instance.Scene.FrameCounter,
                    Name = sfxService.GetSoundEffectName(__instance.Data),
                    Volume = volume,
                    Pitch = pitch,
                    Pan = pan,
                    Data = __instance.Data
                };

                sfxService.AddDesired(sfxToPlay);
            }

            return false;
        }
    }

}
