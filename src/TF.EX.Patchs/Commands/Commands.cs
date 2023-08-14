﻿using MonoMod.Utils;
using TF.EX.Common.Logging;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Services.StateMachine;

namespace TF.EX.Patchs.Commands
{
    public class CommandsPatch : IHookable
    {
        private INetplayStateMachine _stateMachine;
        private INetplayManager _netplayManager;
        private Modes _currrentMode;

        public CommandsPatch(INetplayManager netplayManager, INetplayStateMachine stateMachine)
        {
            _netplayManager = netplayManager;
            _stateMachine = stateMachine;
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
                orig(self, str);
            }
        }

        private void UpdateOpen_Patch(On.Monocle.Commands.orig_UpdateOpen orig, Monocle.Commands self)
        {
            EnsureStateMachine();

            if (ShouldInterceptCommand())
            {
                var clipped = _stateMachine.GetClipped();
                var dynCommands = DynamicData.For(self);
                var currentText = dynCommands.Get<string>("currentText");

                if (!string.IsNullOrEmpty(clipped) && currentText != clipped)
                {
                    currentText = clipped;
                    dynCommands.Set("currentText", currentText);
                }
            }

            orig(self);
        }

        private void ExecuteCommand_Patch(On.Monocle.Commands.orig_ExecuteCommand orig, Monocle.Commands self, string command, string[] args)
        {

            if (ShouldInterceptCommand())
            {
                _stateMachine.UpdateText(command);
            }
            else if (IsWaitingForUserActionOnOptionName(self))
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
            if (ShouldInterceptCommand())
            {
                var dynCommands = DynamicData.For(self);
                var currentText = dynCommands.Get<string>("currentText");
                var commandHistory = dynCommands.Get<List<string>>("commandHistory");
                var drawCommands = dynCommands.Get<List<string>>("drawCommands");

                var command = currentText;
                if (commandHistory.Count == 0 || commandHistory[0] != currentText)
                {
                    commandHistory.Insert(0, currentText);
                    dynCommands.Set("commandHistoryIndex", commandHistory);
                }

                drawCommands.Insert(0, ">" + currentText);
                dynCommands.Set("drawCommands", drawCommands);

                currentText = "";
                dynCommands.Set("currentText", currentText);

                dynCommands.Set("seekIndex", -1);

                Monocle.Engine.Instance.Commands.ExecuteCommand(command, new string[] { });
            }
            else
            {
                orig(self);
            }
        }

        public bool ShouldInterceptCommand()
        {
            try
            {
                var dynMainMenu = DynamicData.For(TowerFall.TFGame.Instance.Scene as TowerFall.MainMenu);
                var state = dynMainMenu.Get<TowerFall.MainMenu.MenuState>("state");

                return state == TowerFall.MainMenu.MenuState.Rollcall && _stateMachine.IsWaitingForUserAction();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsWaitingForUserActionOnOptionName(Monocle.Commands commands)
        {
            var dynCommands = DynamicData.For(commands);
            var drawCommands = dynCommands.Get<List<string>>("drawCommands");

            return string.Join(",", drawCommands).Contains("The name that will be showed as an indicator");
        }

        private void EnsureStateMachine()
        {
            var (stateMachine, mode) = ServiceCollections.ResolveStateMachineService();

            if (_stateMachine is DefaultNetplayStateMachine || _currrentMode != TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel())
            {
                _stateMachine = stateMachine;
                _currrentMode = mode;
            }
        }
    }
}
