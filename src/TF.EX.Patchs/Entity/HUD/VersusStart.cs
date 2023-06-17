using MonoMod.Utils;
using System.Collections;

namespace TF.EX.Patchs.Entity.HUD
{
    public class VersusStartPatch : IHookable
    {
        public static bool IsCoroutineStateRemoved = false;

        public void Load()
        {
            On.TowerFall.VersusStart.SetupSequence += VersusStart_SetupSequence;
        }

        public void Unload()
        {
            On.TowerFall.VersusStart.SetupSequence -= VersusStart_SetupSequence;
        }

        //TODO: re eanble round 0 sequence
        private IEnumerator VersusStart_SetupSequence(On.TowerFall.VersusStart.orig_SetupSequence orig, TowerFall.VersusStart self)
        {
            var dynVersusStart = DynamicData.For(self);

            yield return dynVersusStart.Invoke("IntroSequence");
        }

        public static void UpdateState(bool toUpdate)
        {
            IsCoroutineStateRemoved = toUpdate;
        }
    }
}
