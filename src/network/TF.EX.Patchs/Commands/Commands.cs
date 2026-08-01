using HarmonyLib;
using TF.EX.Domain;

namespace TF.EX.Patchs.Commands
{
    [HarmonyPatch(typeof(Monocle.Commands))]
    public class CommandsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ExecuteCommand")]
        public static bool ExecuteCommand_Patch(Monocle.Commands __instance, string command)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (IsWaitingForUserActionOnOptionName(__instance))
            {
                if (string.IsNullOrEmpty(command))
                {
                    TowerFall.Sounds.ui_invalid.Play();
                }
                else
                {
                    var name = command.ToUpper();
                    var config = netplayManager.GetNetplayMeta();
                    config.Name = name.Substring(0, Math.Min(name.Length, 10));
                    netplayManager.UpdateMeta(config);
                    netplayManager.SaveConfig();
                }

                return false;
            }

            return true;
        }

        private static bool IsWaitingForUserActionOnOptionName(Monocle.Commands commands)
        {
            var drawCommands = Traverse.Create(commands).Field("drawCommands").GetValue<List<string>>();

            return string.Join(",", drawCommands).Contains("The name that will be shown as an indicator");
        }
    }
}
