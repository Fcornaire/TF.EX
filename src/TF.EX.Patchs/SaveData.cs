using HarmonyLib;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(SaveData))]
    internal class SaveDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveData.Save))]
        public static void Save_Patch(SaveData __instance)
        {
            __instance.WithNetplayOptions();
        }
    }
}
