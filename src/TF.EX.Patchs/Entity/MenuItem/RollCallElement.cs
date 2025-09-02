using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(RollcallElement))]
    public class RollCallElementPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Render")]
        public static void RollcallElement_Render()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            if (lobby.Spectators.Any())
            {
                Draw.OutlineTextCentered(TFGame.Font, $"SPECTATORS : {lobby.Spectators.Count}", new Vector2(40f, 20f), Color.White, Color.Black);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("LeaveJoined")]
        public static void RollcallElement_LeaveJoined(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();
            var inputService = ServiceCollections.ResolveInputService();

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var dynRollcallElement = Traverse.Create(__instance);
                var playerIndex = dynRollcallElement.Field<int>("playerIndex").Value;
                StateMachine state = dynRollcallElement.Field<StateMachine>("state").Value;

                //We only care about joined update (1)
                if (state == 1 && MenuInput.Back)
                {
                    if (playerIndex == 0)
                    {
                        var lobby = matchmakingService.GetOwnLobby();
                        var player = lobby.Players.First(pl => pl.RoomChatPeerId == matchmakingService.GetRoomChatPeerId());
                        if (player.Ready)
                        {
                            player.Ready = false;
                            archerService.RemoveArcher(playerIndex);

                            inputService.DisableAllControllers();

                            matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                dynRollcallElement.Field<TowerFall.PlayerInput>("input").Value = TFGame.PlayerInputs[0];
                            }, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                dynRollcallElement.Field<TowerFall.PlayerInput>("input").Value = TFGame.PlayerInputs[0];
                                Sounds.ui_invalid.Play();
                                Notification.Create(__instance.Scene, "Failed to notify server");
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

        [HarmonyPostfix]
        [HarmonyPatch("JoinedUpdate")]
        public static void RollcallElement_JoinedUpdate(RollcallElement __instance, ref int __result)
        {
            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();

            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var playerIndex = Traverse.Create(__instance).Field<int>("playerIndex").Value;

                if (playerIndex == 0)
                {
                    return;
                }

                __result = 1;
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnterJoined")]
        public static void RollcallElement_EnterJoined(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();
            var inputService = ServiceCollections.ResolveInputService();

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            if (currentMode == Domain.Models.Modes.Netplay)
            {
                var lobby = matchmakingService.GetOwnLobby();
                var dynRollcallElement = DynamicData.For(__instance);
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");

                if (playerIndex == 0 && !matchmakingService.IsSpectator())
                {
                    var player = lobby.Players.First(pl => pl.RoomChatPeerId == matchmakingService.GetRoomChatPeerId());
                    if (!player.Ready)
                    {
                        player.Ready = true;
                        player.ArcherIndex = TFGame.Characters[playerIndex];
                        player.ArcherAltIndex = (int)TFGame.AltSelect[playerIndex];

                        archerService.AddArcher(playerIndex, player);

                        inputService.DisableAllControllers();

                        matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                dynRollcallElement.Set("input", TFGame.PlayerInputs[0]);
                                __instance.HandleControlChange();
                            }, () =>
                            {
                                inputService.EnableAllControllers();
                                inputService.DisableAllControllerExceptLocal();
                                dynRollcallElement.Set("input", TFGame.PlayerInputs[0]);

                                __instance.HandleControlChange();
                                Sounds.ui_invalid.Play();
                                Notification.Create(__instance.Scene, "Failed to notify server");
                            });
                    }
                }
                else
                {
                    UpdatePlayerUI(dynRollcallElement);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("NotJoinedUpdate")]
        public static void RollcallElement_NotJoinedUpdate(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var dynRollcallElement = DynamicData.For(__instance);

            int playerIndex = dynRollcallElement.Get<int>("playerIndex");

            if (playerIndex == 0)
            {
                var input = dynRollcallElement.Get<TowerFall.PlayerInput>("input");

                var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();

                if (currentMode == Domain.Models.Modes.Netplay && input != null && input.MenuBack)
                {
                    Task.Run(async () =>
                    {
                        await matchmakingService.LeaveLobby(() =>
                        {
                            (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                            if (!matchmakingService.IsSpectator())
                            {
                                matchmakingService.DisconnectFromLobby();
                            }
                            matchmakingService.ResetPeer();
                        }, () =>
                        {
                            (TFGame.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
                        });
                    }).GetAwaiter().GetResult();

                    (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void RollcallElement_Update(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            var dynRollcallElement = DynamicData.For(__instance);
            int playerIndex = dynRollcallElement.Get<int>("playerIndex");

            if (playerIndex != 0 && playerIndex < lobby.Players.Count)
            {
                var player = lobby.Players.ToArray()[playerIndex];
                if (player != null)
                {
                    UpdateControllerIcon(__instance, dynRollcallElement, playerIndex);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("StartVersus")]
        public static void RollcallElement_StartVersus_Prefix()
        {
            TFGame.Instance.Commands.Clear();
            TFGame.Instance.Commands.Open = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("StartVersus")]
        public static void RollcallElement_StartVersus_Postfix(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var currentMode = MainMenu.VersusMatchSettings.Mode.ToModel();
            var lobby = matchmakingService.GetOwnLobby();
            if (currentMode.IsNetplay() && lobby.Spectators.Any())
            {
                netplayManager.AddSpectators(lobby.Spectators);
            }

            netplayManager.UpdateNumPlayers(lobby.Players.Count);

            if (MainMenu.VersusMatchSettings.TeamMode)
            {
                __instance.MainMenu.State = MainMenu.MenuState.TeamSelect;
                return;
            }

            __instance.MainMenu.FadeAction = MainMenu.GotoVersusLevelSelect;
            __instance.MainMenu.State = MainMenu.MenuState.Fade;
        }

        private static void UpdatePlayerUI(DynamicData rollcallElement)
        {
            var playerIndex = rollcallElement.Get<int>("playerIndex");
            rollcallElement.Set("archerType", TFGame.AltSelect[playerIndex]);
            var portrait = rollcallElement.Get<ArcherPortrait>("portrait");
            var archerType = rollcallElement.Get<ArcherData.ArcherTypes>("archerType");

            portrait.SetCharacter(TFGame.Characters[playerIndex], archerType, 1);
            portrait.Join(unlock: false);
            TFGame.Players[playerIndex] = true;
        }

        private static void LeavePlayerUI(Traverse rollcallElement)
        {
            var playerIndex = rollcallElement.Field<int>("playerIndex").Value;
            var portrait = rollcallElement.Field<ArcherPortrait>("portrait").Value;

            portrait.Leave();
            TFGame.Players[playerIndex] = false;
        }

        private static void UpdateControllerIcon(RollcallElement self, DynamicData rollcallElement, int playerIndex)
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
