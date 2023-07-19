using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.SFX
{
    public class SFXPatch : IHookable
    {
        private readonly ISFXService _sfxService;
        private readonly INetplayManager _netplayManager;

        public SFXPatch(ISFXService sFXService, INetplayManager netplayManager)
        {
            _sfxService = sFXService;
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.Monocle.SFX.Play += SFX_Play;
            On.Monocle.SFX.ctor_string_bool += SFX_ctor_string_bool;
        }

        public void Unload()
        {
            On.Monocle.SFX.Play -= SFX_Play;
            On.Monocle.SFX.ctor_string_bool -= SFX_ctor_string_bool;
        }

        private void SFX_ctor_string_bool(On.Monocle.SFX.orig_ctor_string_bool orig, Monocle.SFX self, string filename, bool obeysMasterPitch)
        {
            orig(self, filename, obeysMasterPitch);
            var dynSFX = DynamicData.For(self);

            dynSFX.Add("SFXName", filename);
            _sfxService.AddSoundEffect(self.Data, filename);
        }

        private void SFX_Play(On.Monocle.SFX.orig_Play orig, Monocle.SFX self, float panX, float volume)
        {
            if (!_netplayManager.IsInit())
            {
                orig(self, panX, volume);
                return;
            }

            if (_netplayManager.IsUpdating())
            {
                return; //Ignore SFXs on the first frame of a rollback (Coroutines update might play a sound)
            }

            var dynSFX = DynamicData.For(self);

            if (self.Data != null && Monocle.Audio.MasterVolume > 0f)
            {
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                volume *= Monocle.Audio.MasterVolume;
                var pitch = self.ObeysMasterPitch ? Monocle.Audio.MasterPitch : 0f;
                var pan = Monocle.SFX.CalculatePan(panX);

                var sfxToPlay = new Domain.Models.State.SFX
                {
                    Frame = (int)Monocle.Engine.Instance.Scene.FrameCounter,
                    Name = dynSFX.Get<string>("SFXName"),
                    Volume = volume,
                    Pitch = pitch,
                    Pan = pan,
                    Data = self.Data
                };

                _sfxService.AddDesired(sfxToPlay);
            }
        }
    }

}
