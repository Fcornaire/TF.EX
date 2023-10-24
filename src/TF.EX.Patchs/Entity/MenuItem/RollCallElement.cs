using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    public class RollCallElementPatch : IHookable
    {
        private readonly IMatchmakingService _matchmakingService;
        private readonly IArcherService archerService;
        private readonly IInputService inputService;
        private readonly INetplayManager netplayManager;

        public RollCallElementPatch(IMatchmakingService matchmakingService,
            IArcherService archerService,
            IInputService inputService,
            INetplayManager netplayManager)
        {
            _matchmakingService = matchmakingService;
            this.archerService = archerService;
            this.inputService = inputService;
            this.netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.RollcallElement.StartVersus += RollcallElement_StartVersus;
            On.TowerFall.RollcallElement.Update += RollcallElement_Update;
            On.TowerFall.RollcallElement.NotJoinedUpdate += RollcallElement_NotJoinedUpdate;
            On.TowerFall.RollcallElement.EnterJoined += RollcallElement_EnterJoined;
            On.TowerFall.RollcallElement.JoinedUpdate += RollcallElement_JoinedUpdate;
            On.TowerFall.RollcallElement.LeaveJoined += RollcallElement_LeaveJoined;
            On.TowerFall.RollcallElement.Render += RollcallElement_Render;
        }

        public void Unload()
        {
            On.TowerFall.RollcallElement.StartVersus -= RollcallElement_StartVersus;
            On.TowerFall.RollcallElement.Update -= RollcallElement_Update;
            On.TowerFall.RollcallElement.NotJoinedUpdate -= RollcallElement_NotJoinedUpdate;
            On.TowerFall.RollcallElement.EnterJoined -= RollcallElement_EnterJoined;
            On.TowerFall.RollcallElement.JoinedUpdate -= RollcallElement_JoinedUpdate;
            On.TowerFall.RollcallElement.LeaveJoined -= RollcallElement_LeaveJoined;
            On.TowerFall.RollcallElement.Render -= RollcallElement_Render;
        }

        private void RollcallElement_Render(On.TowerFall.RollcallElement.orig_Render orig, RollcallElement self)
        {
            orig(self);

            var lobby = _matchmakingService.GetOwnLobby();
            if (lobby.Spectators.Any())
            {
                Draw.OutlineTextCentered(TFGame.Font, $"SPECTATORS : {lobby.Spectators.Count}", new Vector2(40f, 20f), Color.White, Color.Black);
            }
        }


        private void RollcallElement_LeaveJoined(On.TowerFall.RollcallElement.orig_LeaveJoined orig, RollcallElement self)
        {
            orig(self);

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var dynRollcallElement = DynamicData.For(self);
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");
                StateMachine state = dynRollcallElement.Get<StateMachine>("state");

                //We only care about joined update (1)
                if (state == 1 && MenuInput.Back)
                {
                    if (playerIndex == 0)
                    {
                        var lobby = _matchmakingService.GetOwnLobby();
                        var player = lobby.Players.First(pl => pl.RoomChatPeerId == _matchmakingService.GetRoomChatPeerId());
                        if (player.Ready)
                        {
                            player.Ready = false;
                            archerService.RemoveArcher(playerIndex);

                            inputService.DisableAllControllers();

                            _matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                            }, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                Sounds.ui_invalid.Play();
                                Notification.Create(self.Scene, "Failed to notify server");
                            });
                        }
                    }
                    else
                    {
                        LeavePlayerUI(dynRollcallElement);
                    }
                }
            }
        }

        private int RollcallElement_JoinedUpdate(On.TowerFall.RollcallElement.orig_JoinedUpdate orig, RollcallElement self)
        {
            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var dynRollcallElement = DynamicData.For(self);
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");

                if (playerIndex == 0)
                {
                    return orig(self);
                }

                return 1;
            }

            return orig(self);
        }

        private void RollcallElement_EnterJoined(On.TowerFall.RollcallElement.orig_EnterJoined orig, RollcallElement self)
        {
            orig(self);

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var lobby = _matchmakingService.GetOwnLobby();
                var dynRollcallElement = DynamicData.For(self);
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");

                if (playerIndex == 0 && !_matchmakingService.IsSpectator())
                {
                    var player = lobby.Players.First(pl => pl.RoomChatPeerId == _matchmakingService.GetRoomChatPeerId());
                    if (!player.Ready)
                    {
                        player.Ready = true;
                        player.ArcherIndex = TFGame.Characters[playerIndex];
                        player.ArcherAltIndex = (int)TFGame.AltSelect[playerIndex];

                        archerService.AddArcher(playerIndex, player);

                        inputService.DisableAllControllers();

                        _matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                self.HandleControlChange();
                            }, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                self.HandleControlChange();
                                Sounds.ui_invalid.Play();
                                Notification.Create(self.Scene, "Failed to notify server");
                            });
                    }
                }
                else
                {
                    UpdatePlayerUI(dynRollcallElement);
                }
            }
        }

        private int RollcallElement_NotJoinedUpdate(On.TowerFall.RollcallElement.orig_NotJoinedUpdate orig, RollcallElement self)
        {
            var dynRollcallElement = DynamicData.For(self);

            var res = orig(self);

            var input = dynRollcallElement.Get<TowerFall.PlayerInput>("input");

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode == Domain.Models.Modes.Netplay && input != null && input.MenuBack)
            {
                Task.Run(async () =>
                {
                    await _matchmakingService.LeaveLobby(() =>
                    {
                        (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                        if (!_matchmakingService.IsSpectator())
                        {
                            _matchmakingService.DisconnectFromLobby();
                        }
                        _matchmakingService.ResetPeer();
                    }, () =>
                    {
                        (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                    });
                }).GetAwaiter().GetResult();

                (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
            }

            return res;
        }

        private void RollcallElement_Update(On.TowerFall.RollcallElement.orig_Update orig, RollcallElement self)
        {
            var lobby = _matchmakingService.GetOwnLobby();
            var dynRollcallElement = DynamicData.For(self);
            int playerIndex = dynRollcallElement.Get<int>("playerIndex");

            if (playerIndex != 0 && playerIndex < lobby.Players.Count)
            {
                var player = lobby.Players.ToArray()[playerIndex];
                if (player != null)
                {
                    UpdateControllerIcon(self, dynRollcallElement, playerIndex);
                }
            }

            orig(self);
        }

        private void RollcallElement_StartVersus(On.TowerFall.RollcallElement.orig_StartVersus orig, RollcallElement self)
        {
            TFGame.Instance.Commands.Clear();
            TFGame.Instance.Commands.Open = false;

            orig(self);

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            var lobby = _matchmakingService.GetOwnLobby();
            if (currentMode.IsNetplay() && lobby.Spectators.Any())
            {
                netplayManager.AddSpectators(lobby.Spectators);
            }

            netplayManager.UpdateNumPlayers(lobby.Players.Count);

            if (MainMenu.VersusMatchSettings.TeamMode)
            {
                self.MainMenu.State = MainMenu.MenuState.TeamSelect;
                return;
            }

            self.MainMenu.FadeAction = MainMenu.GotoVersusLevelSelect;
            self.MainMenu.State = MainMenu.MenuState.Fade;
        }

        private void UpdatePlayerUI(DynamicData rollcallElement)
        {
            var playerIndex = rollcallElement.Get<int>("playerIndex");
            rollcallElement.Set("archerType", TFGame.AltSelect[playerIndex]);
            var portrait = rollcallElement.Get<ArcherPortrait>("portrait");
            var archerType = rollcallElement.Get<ArcherData.ArcherTypes>("archerType");

            portrait.SetCharacter(TFGame.Characters[playerIndex], archerType, 1);
            portrait.Join(unlock: false);
            TFGame.Players[playerIndex] = true;
        }

        private void LeavePlayerUI(DynamicData rollcallElement)
        {
            var playerIndex = rollcallElement.Get<int>("playerIndex");
            var portrait = rollcallElement.Get<ArcherPortrait>("portrait");

            portrait.Leave();
            TFGame.Players[playerIndex] = false;
        }

        private void UpdateControllerIcon(RollcallElement self, DynamicData rollcallElement, int playerIndex)
        {
            var alt = TFGame.MenuAtlas[$"controls/xb360/rt"];
            var controlIcon = rollcallElement.Get<Image>("controlIcon");

            rollcallElement.Set("confirmButton", TFGame.MenuAtlas[$"controls/xb360/a"]);
            rollcallElement.Set("altButton", alt);
            rollcallElement.Set("altBezier", new Bezier(new Vector2(30 - alt.Width / 2, 80f), new Vector2(-30 + alt.Width / 2, 80f), new Vector2(0f, 120f)));

            controlIcon.SwapSubtexture(TFGame.MenuAtlas[$"controls/xb360/player{playerIndex + 1}"]);
            controlIcon.CenterOrigin();

            rollcallElement.Set("controlIcon", controlIcon);
        }
    }
}
