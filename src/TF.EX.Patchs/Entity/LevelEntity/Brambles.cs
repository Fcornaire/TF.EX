using HarmonyLib;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Brambles))]
    public class BramblesPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Brambles.Removed))]
        public static void Brambles_Removed(Brambles __instance)
        {
            var dynBrambles = DynamicData.For(__instance);
            Stack<Brambles> cached = dynBrambles.Get<Stack<Brambles>>("cached");
            if (cached != null)
            {
                cached.Clear();
            }
        }
    }
}
