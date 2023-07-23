using MonoMod.Utils;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    internal class VersusRoundResultsPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.VersusRoundResults.Update += VersusRoundResults_Update;
        }

        public void Unload()
        {
            On.TowerFall.VersusRoundResults.Update -= VersusRoundResults_Update;
        }

        private void VersusRoundResults_Update(On.TowerFall.VersusRoundResults.orig_Update orig, TowerFall.VersusRoundResults self)
        {
            orig(self);

            var dynVersusRoundResults = DynamicData.For(self);
            var finished = dynVersusRoundResults.Get<bool>("finished");
            if (finished)
            {
                var miasma = (TFGame.Instance.Scene as Level).Get<Miasma>(); //Also manually removing the miasma
                if (miasma != null)
                {
                    miasma.RemoveSelf();
                }

                (TFGame.Instance.Scene as Level).DeleteAll<Arrow>();
                (TFGame.Instance.Scene as Level).DeleteAll<PlayerCorpse>();
                (TFGame.Instance.Scene as Level).DeleteAll<Pickup>();
            }
        }
    }
}
