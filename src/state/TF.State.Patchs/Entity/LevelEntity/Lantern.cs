using HarmonyLib;
using MonoMod.Utils;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(TowerFall.Lantern))]
    public class LanternPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SceneBegin")]
        public static void Lantern_SceneBegin_Postfix(TowerFall.Lantern __instance)
        {
            DynamicData.For(__instance).Set("homeChain", __instance.Chain);
        }
    }
}
