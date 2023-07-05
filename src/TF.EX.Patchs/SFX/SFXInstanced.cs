using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.SFX
{
    public class SFXInstancedPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly ISFXService _sfxService;

        public SFXInstancedPatch(INetplayManager netplayManager, ISFXService sfxService)
        {
            _netplayManager = netplayManager;
            _sfxService = sfxService;
        }

        public void Load()
        {
            On.Monocle.SFXInstanced.Play += SFXInstanced_Play;
        }

        public void Unload()
        {
            On.Monocle.SFXInstanced.Play -= SFXInstanced_Play;
        }

        private void SFXInstanced_Play(On.Monocle.SFXInstanced.orig_Play orig, Monocle.SFXInstanced self, float panX, float volume)
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

            if (self.Data != null && !(Audio.MasterVolume <= 0f))
            {
                dynSFX.Invoke("AddToPlayedList", panX, volume);

                volume *= Audio.MasterVolume;

                var sfxToPlay = new Domain.Models.State.SFX
                {
                    Frame = (int)Monocle.Engine.Instance.Scene.FrameCounter,
                    Name = dynSFX.Get<string>("SFXName"),
                    Volume = volume,
                    Pan = Monocle.SFX.CalculatePan(panX),
                    Data = self.Data
                };

                _sfxService.AddDesired(sfxToPlay);
            }
        }
    }
}
