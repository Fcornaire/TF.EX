using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(LevelLoaderXML))]
    public class LevelLoaderXMLPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void LevelLoaderXML_Update(LevelLoaderXML __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (__instance.Finished
                && TowerFall.MainMenu.VersusMatchSettings.Mode.IsNetplay()
                && !netplayManager.IsReplayMode()
                && !netplayManager.IsSynchronized()
                && __instance.Level.Session.RoundIndex == 0
                )
            {
                Notification.Create(__instance.Level, "Waiting for other players...", 20, 0, false, true);
            }
        }

    }
}
