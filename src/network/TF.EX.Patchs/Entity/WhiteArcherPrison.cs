using HarmonyLib;
using Monocle;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(WhiteArcherPrison))]
    public class WhiteArcherPrisonPatch
    {
        //We dont it to spawn on netplay or othter determinist path
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WhiteArcherPrison.Spawn))]
        public static bool WhiteArcherPrison_Spawn(System.Xml.XmlElement xml, ref Monocle.Entity __result)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var mode = TowerFall.MainMenu.VersusMatchSettings?.Mode;

            var deterministic = (mode != null && mode.Value.IsNetplay())
                || netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Test
                || netplayManager.IsReplayMode();

            if (!deterministic)
            {
                return true;
            }

            __result = new WhiteArcherPrisonBroken(xml.Position());

            return false;
        }
    }
}
