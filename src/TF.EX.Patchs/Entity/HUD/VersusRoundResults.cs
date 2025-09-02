using HarmonyLib;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    [HarmonyPatch(typeof(VersusRoundResults))]
    internal class VersusRoundResultsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void VersusRoundResults_Update(VersusRoundResults __instance)
        {
            var finished = Traverse.Create(__instance).Field("finished").GetValue<bool>();
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
