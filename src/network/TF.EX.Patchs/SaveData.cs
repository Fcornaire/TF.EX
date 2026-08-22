using HarmonyLib;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(SaveData))]
    internal class SaveDataPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SaveData.Save))]
        public static void Save_Prefix() => NetplayOptions.BeforeSave();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(SaveData.Save))]
        public static void Save_Postfix() => NetplayOptions.AfterSave();
    }
}
