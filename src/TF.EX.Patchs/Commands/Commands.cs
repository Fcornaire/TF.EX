using MonoMod.Utils;
using TF.EX.Common.Logging;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Commands
{
    public class CommandsPatch : IHookable
    {
        private INetplayManager _netplayManager;
        private Modes _currrentMode;

        public CommandsPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.Monocle.Commands.ExecuteCommand += ExecuteCommand_Patch;
            On.Monocle.Commands.EnterCommand += EnterCommand_Patch;
            On.Monocle.Commands.UpdateOpen += UpdateOpen_Patch;
            On.Monocle.Commands.Log += Commands_Log;
        }

        public void Unload()
        {
            On.Monocle.Commands.ExecuteCommand -= ExecuteCommand_Patch;
            On.Monocle.Commands.EnterCommand -= EnterCommand_Patch;
            On.Monocle.Commands.UpdateOpen -= UpdateOpen_Patch;
            On.Monocle.Commands.Log -= Commands_Log;
        }

        private void Commands_Log(On.Monocle.Commands.orig_Log orig, Monocle.Commands self, string str)
        {
            if (!Logger.ShouldIgnoreCommandLog)
            {
                try
                {
                    orig(self, str);
                }
                catch (System.Exception)
                {
                    //ignore
                }
            }
        }

        private void UpdateOpen_Patch(On.Monocle.Commands.orig_UpdateOpen orig, Monocle.Commands self)
        {
            orig(self);
        }

        private void ExecuteCommand_Patch(On.Monocle.Commands.orig_ExecuteCommand orig, Monocle.Commands self, string command, string[] args)
        {
            if (IsWaitingForUserActionOnOptionName(self))
            {
                if (string.IsNullOrEmpty(command))
                {
                    TowerFall.Sounds.ui_invalid.Play();
                }
                else
                {
                    var name = command.ToUpper();
                    var config = _netplayManager.GetNetplayMeta();
                    config.Name = name.Substring(0, Math.Min(name.Length, 10));
                    _netplayManager.UpdateMeta(config);
                    _netplayManager.SaveConfig();
                }
            }
            else
            {
                orig(self, command, args);
            }
        }

        private void EnterCommand_Patch(On.Monocle.Commands.orig_EnterCommand orig, Monocle.Commands self)
        {
            //if (ShouldInterceptCommand())
            //{
            //    var dynCommands = DynamicData.For(self);
            //    var currentText = dynCommands.Get<string>("currentText");
            //    var commandHistory = dynCommands.Get<List<string>>("commandHistory");
            //    var drawCommands = dynCommands.Get<List<string>>("drawCommands");

            //    var command = currentText;
            //    if (commandHistory.Count == 0 || commandHistory[0] != currentText)
            //    {
            //        commandHistory.Insert(0, currentText);
            //        dynCommands.Set("commandHistoryIndex", commandHistory);
            //    }

            //    drawCommands.Insert(0, ">" + currentText);
            //    dynCommands.Set("drawCommands", drawCommands);

            //    currentText = "";
            //    dynCommands.Set("currentText", currentText);

            //    dynCommands.Set("seekIndex", -1);

            //    Monocle.Engine.Instance.Commands.ExecuteCommand(command, new string[] { });
            //}
            //else
            //{
            orig(self);
            //}
        }

        private bool IsWaitingForUserActionOnOptionName(Monocle.Commands commands)
        {
            var dynCommands = DynamicData.For(commands);
            var drawCommands = dynCommands.Get<List<string>>("drawCommands");

            return string.Join(",", drawCommands).Contains("The name that will be showed as an indicator");
        }
    }
}
