using HarmonyLib;
using Monocle;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(Variant))]
    internal class VariantPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Subtexture), typeof(string), typeof(string), typeof(Pickups[]), typeof(bool), typeof(string), typeof(TowerFall.UnlockData.Unlocks?), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int)])]
        public static void Variant_ctor(ref bool perPlayer)
        {
            perPlayer = false;
        }
    }
}
