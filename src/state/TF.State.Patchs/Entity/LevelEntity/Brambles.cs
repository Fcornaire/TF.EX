using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Brambles))]
    public class BramblesPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Brambles.Removed))]
        public static void Brambles_Removed(Brambles __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var dynBrambles = DynamicData.For(__instance);
            Stack<Brambles> cached = dynBrambles.Get<Stack<Brambles>>("cached");
            if (cached != null)
            {
                cached.Clear();
            }
        }
    }
}
