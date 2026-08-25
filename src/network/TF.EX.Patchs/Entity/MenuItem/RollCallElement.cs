using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using TF.EX.Common.Extensions;
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
        public static void RollcallElement_Render(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var lobby = matchmakingService.GetOwnLobby();
            if (lobby.Spectators.Any())
            {
                Draw.OutlineTextCentered(TFGame.Font, $"SPECTATORS : {lobby.Spectators.Count}", new Vector2(40f, 20f), Color.White, Color.Black);
            }

            var dynRollcall = DynamicData.For(__instance);

            if (lobby.IsEmpty)
            {
                return;
            }

            var seat = dynRollcall.Get<int>("playerIndex");
            var seated = lobby.Players.FirstOrDefault(player => player.Seat == seat);

            if (seated == null)
            {
                return;
            }

            var input = dynRollcall.Get<TowerFall.PlayerInput>("input");
            var name = !string.IsNullOrEmpty(seated.Name) ? seated.Name : input?.Name;

            if ((input == null || RollcallLayout.IsWide) && !string.IsNullOrEmpty(name))
            {
                var archer = ArcherData.Archers[__instance.CharacterIndex];
                var nameColor = dynRollcall.Get<StateMachine>("state").State == 1 ? archer.ColorB : archer.ColorA;

                Draw.OutlineTextCentered(TFGame.Font, name, RollcallLayout.NameAt(__instance), nameColor, Color.Black);
            }

            if (!lobby.IsTeamMode)
            {
                return;
            }

            var team = (Allegiance)seated.Team;
            var label = team == Allegiance.Blue ? "BLUE" : team == Allegiance.Red ? "RED" : "UP/DOWN";
            var color = team == Allegiance.Blue ? Color.CornflowerBlue
                : team == Allegiance.Red ? Color.IndianRed
                : Color.Gray;

            Draw.OutlineTextCentered(TFGame.Font, label, RollcallLayout.TeamAt(__instance), color, Color.Black);
        }

        [HarmonyPostfix]
        [HarmonyPatch("LeaveJoined")]
        public static void RollcallElement_LeaveJoined(RollcallElement __instance)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var archerService = ServiceCollections.ResolveArcherService();
            var inputService = ServiceCollections.ResolveInputService();

            var currentMode = MainMenu.VersusMatchSettings?.Mode;
            var lobby = matchmakingService.GetOwnLobby();

            if (currentMode?.IsNetplay() == true && !lobby.IsEmpty)
            {
                var dynRollcallElement = Traverse.Create(__instance);
                var playerIndex = dynRollcallElement.Field<int>("playerIndex").Value;
                StateMachine state = dynRollcallElement.Field<StateMachine>("state").Value;

                var elementInput = dynRollcallElement.Field<TowerFall.PlayerInput>("input").Value;

                if (state.State == 1 && elementInput != null && elementInput.MenuBack)
                {
                    if (playerIndex == matchmakingService.GetLocalSeat())
                    {
                        var player = lobby.Players.FirstOrDefault(pl => pl.RoomPeerId == matchmakingService.GetRoomPeerId());
                        if (player != null && player.Ready)
                        {
                            player.Ready = false;
                            archerService.RemoveArcher(playerIndex);

                            inputService.DisableAllControllers();

                            matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                            }, () =>
                            {
                                inputService.EnableAllControllers();
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

        [HarmonyPrefix]
        [HarmonyPatch("NotJoinedUpdate")]
        public static bool RollcallElement_NotJoinedUpdate_Prefix(RollcallElement __instance, ref int __result)
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (matchmakingService.GetOwnLobby().IsEmpty || MainMenu.VersusMatchSettings?.Mode.IsNetplay() != true)
            {
                return true;
            }

            var input = DynamicData.For(__instance).Get<TowerFall.PlayerInput>("input");

            if (input == null || !input.MenuBack)
            {
                return true;
            }

            __result = 0;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("JoinedUpdate")]
        public static bool RollcallElement_JoinedUpdate_Prefix(RollcallElement __instance, ref int __result)
        {
            if (MainMenu.VersusMatchSettings?.Mode.IsNetplay() != true || ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty)
            {
                return true;
            }

            if (DynamicData.For(__instance).Get<TowerFall.PlayerInput>("input") != null)
            {
                return true;
            }

            __result = 1;
            return false;
        }


        [HarmonyPostfix]
        [HarmonyPatch("JoinedUpdate")]
        public static void RollcallElement_JoinedUpdate(RollcallElement __instance, ref int __result)
        {
            var currentMode = MainMenu.VersusMatchSettings?.Mode;
            var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();

            if (lobby.IsEmpty)
            {
                return;
            }

            if (currentMode?.IsNetplay() == true)
            {
                var playerIndex = Traverse.Create(__instance).Field<int>("playerIndex").Value;

                if (playerIndex == ServiceCollections.ResolveMatchmakingService().GetLocalSeat())
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

            var currentMode = MainMenu.VersusMatchSettings?.Mode;
            var lobby = matchmakingService.GetOwnLobby();
            if (currentMode?.IsNetplay() == true && !lobby.IsEmpty)
            {
                var dynRollcallElement = DynamicData.For(__instance);
                var playerIndex = dynRollcallElement.Get<int>("playerIndex");

                if (playerIndex == matchmakingService.GetLocalSeat() && !matchmakingService.IsSpectator())
                {
                    var player = lobby.Players.FirstOrDefault(pl => pl.RoomPeerId == matchmakingService.GetRoomPeerId());
                    if (player != null && !player.Ready)
                    {
                        player.Ready = true;
                        player.ArcherIndex = TFGame.Characters[playerIndex];
                        player.ArcherAltIndex = (int)TFGame.AltSelect[playerIndex];

                        archerService.AddArcher(playerIndex, player);

                        inputService.DisableAllControllers();

                        matchmakingService.UpdatePlayer(player, () =>
                            {
                                inputService.EnableAllControllers();
                            }, () =>
                            {
                                inputService.EnableAllControllers();
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

            if (playerIndex == matchmakingService.GetLocalSeat())
            {
                var input = dynRollcallElement.Get<TowerFall.PlayerInput>("input");

                var currentMode = MainMenu.VersusMatchSettings?.Mode;
                var lobby = matchmakingService.GetOwnLobby();

                var isJoined = dynRollcallElement.Get<Monocle.StateMachine>("state").State != 0;

                if (currentMode?.IsNetplay() == true && !lobby.IsEmpty && !isJoined && input != null && input.MenuBack)
                {
                    Task.Run(async () =>
                    {
                        await matchmakingService.LeaveLobby(() =>
                        {
                            (TFGame.Instance.Scene as MainMenu).State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
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

            if (!lobby.IsEmpty && playerIndex >= 0 && playerIndex < TFGame.PlayerInputs.Length)
            {
                var owned = TFGame.PlayerInputs[playerIndex];

                if (owned != null && dynRollcallElement.Get<TowerFall.PlayerInput>("input") != owned)
                {
                    dynRollcallElement.Set("input", owned);
                }
            }

            if (playerIndex == matchmakingService.GetLocalSeat())
            {
                HandleTeamSelection(__instance, dynRollcallElement, lobby);
            }
            else if (lobby.Players.Any(player => player.Seat == playerIndex))
            {
                UpdateControllerIcon(__instance, dynRollcallElement, playerIndex);
            }

            UpdateOpenSeat(__instance, dynRollcallElement, lobby, playerIndex);
        }

        private static void UpdateOpenSeat(RollcallElement element, DynamicData dynRollcallElement, Domain.Models.WebSocket.Lobby lobby, int playerIndex)
        {
            var controlIcon = dynRollcallElement.Get<Monocle.Image>("controlIcon");

            if (controlIcon == null)
            {
                return;
            }

            var orb = dynRollcallElement.Get("openSeatOrb") as Monocle.Sprite<int>;

            var openSeat = !lobby.IsEmpty
                && playerIndex < lobby.MaxPlayers
                && !lobby.Players.Any(player => player.Seat == playerIndex);

            if (openSeat && orb == null)
            {
                orb = TFGame.SpriteData.GetSpriteInt("ChaosOrb");
                orb.Play(0);
                orb.CenterOrigin();
                orb.Position = controlIcon.Position;
                element.Add(orb);
                dynRollcallElement.Set("openSeatOrb", orb);
            }

            if (orb != null)
            {
                orb.Visible = openSeat;
                controlIcon.Visible = !openSeat;
            }
        }

        private static void HandleTeamSelection(RollcallElement element, DynamicData dynRollcallElement, Domain.Models.WebSocket.Lobby lobby)
        {
            if (!lobby.IsTeamMode)
            {
                return;
            }

            var input = dynRollcallElement.Get<TowerFall.PlayerInput>("input");

            if (input == null)
            {
                return;
            }

            var chosen = input.MenuUp ? Allegiance.Blue
                : input.MenuDown ? Allegiance.Red
                : Allegiance.Neutral;

            if (chosen == Allegiance.Neutral)
            {
                return;
            }

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();
            var inputService = ServiceCollections.ResolveInputService();

            var player = lobby.Players.FirstOrDefault(pl => pl.RoomPeerId == matchmakingService.GetRoomPeerId());

            if (player == null || player.Team == (int)chosen)
            {
                return;
            }

            player.Team = (int)chosen;
            Sounds.ui_move2.Play();

            inputService.DisableAllControllers();

            matchmakingService.UpdatePlayer(player, () =>
            {
                inputService.EnableAllControllers();
            }, () =>
            {
                inputService.EnableAllControllers();
                Sounds.ui_invalid.Play();
                Notification.Create(element.Scene, "Failed to notify server");
            });
        }

        [HarmonyPrefix]
        [HarmonyPatch("ForceStart")]
        public static bool RollcallElement_ForceStart_Prefix()
        {
            return ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsEmpty;
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

            if (matchmakingService.GetOwnLobby().IsEmpty)
            {
                return;
            }

            var currentMode = MainMenu.VersusMatchSettings?.Mode;
            var lobby = matchmakingService.GetOwnLobby();
            if (currentMode?.IsNetplay() == true && lobby.Spectators.Any())
            {
                netplayManager.AddSpectators(lobby.Spectators);
            }

            netplayManager.UpdateNumPlayers(lobby.Players.Count);

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

            var owner = portrait.Entity;
            var position = owner?.Position ?? Vector2.Zero;

            if (owner != null)
            {
                owner.Position = new Vector2(MathHelper.Clamp(position.X, 0f, 320f), position.Y);
            }

            try
            {
                portrait.Join(unlock: false);
            }
            catch (Exception e)
            {
                var logger = ServiceCollections.ResolveLogger();
                logger.LogWarning($"Failed to join portrait for player {playerIndex}: {e}");
            }
            finally
            {
                if (owner != null)
                {
                    owner.Position = position;
                }
            }

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
