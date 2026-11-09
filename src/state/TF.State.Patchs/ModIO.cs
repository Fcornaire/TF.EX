using System.Xml;
using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs
{
    [HarmonyPatch(typeof(FortRise.ModIO))]
    public class ModIOPatch
    {
        private const string FlightLevel07 = "Content/Levels/Versus/03 - Flight/07.oel";

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FortRise.ModIO.LoadXml), typeof(string))]
        public static void ModIO_LoadXml(ref string path)
        {
            if (StateFlags.IsCaptureActive && !OperatingSystem.IsWindows())
            {
                path = path.Replace('\\', '/');
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(FortRise.ModIO.LoadXml), typeof(string))]
        public static void ModIO_LoadXml_Postfix(string path, XmlDocument __result)
        {
            if (!StateFlags.IsCaptureActive || OperatingSystem.IsWindows() || !path.Replace('\\', '/').EndsWith(FlightLevel07, StringComparison.Ordinal))
            {
                return;
            }

            //Linux/macOS version of this level has an extra chest (not on Windows) that desync the treasure spawn
            var extraChest = __result["level"]?["Entities"]?.ChildNodes
                .OfType<XmlElement>()
                .FirstOrDefault(entity => entity.Name == "TreasureChest" && entity.GetAttribute("x") == "160" && entity.GetAttribute("y") == "150");

            extraChest?.ParentNode.RemoveChild(extraChest);
        }
    }
}
