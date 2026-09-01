using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LightPauseMenu : Entity
    {
        private enum MenuButton { Confirm, Back, Up, Down, Start }

        private const int HUD_LAYER = 4;
        private const float TOP = 96f;
        private const float LINE_HEIGHT = 12f;

        private static readonly string[] Options = { "RESUME", "QUIT" };
        private static readonly Vector2 JustifyVec = new(0.5f, 0.5f);

        private int selectedIndex;
        private bool quitRequested;
        private bool skipOpeningFrame = true;

        private static LightPauseMenu live;

        public static bool IsOpen { get; private set; }

        private LightPauseMenu() : base(HUD_LAYER)
        {
            Depth = -2000000;
        }

        public static void HandleStartPress(Level level)
        {
            if (IsOpen)
            {
                if (live == null || live.Scene != level) //round change
                {
                    Attach(level);
                }

                return;
            }

            if (level.Ending || level.Session.GetWinner() != -1 || !Pressed(MenuButton.Start))
            {
                return;
            }

            IsOpen = true;
            Sounds.ui_pause.Play();
            Attach(level);
        }

        public static void ForceClose()
        {
            IsOpen = false;
            live = null;
        }

        private static void Attach(Level level)
        {
            live = new LightPauseMenu();
            level.Add(live);
        }

        public override void Removed()
        {
            base.Removed();

            if (live == this)
            {
                live = null;
            }
        }

        public override void SceneEnd()
        {
            base.SceneEnd();

            if (live == this)
            {
                live = null;
            }
        }

        public override void Update()
        {
            base.Update();

            if (quitRequested)
            {
                return;
            }

            if (skipOpeningFrame)
            {
                skipOpeningFrame = false;
                return;
            }

            if (Scene is Level level && level.Session.GetWinner() != -1)
            {
                Close();
                return;
            }

            if (Pressed(MenuButton.Up) || Pressed(MenuButton.Down))
            {
                selectedIndex = 1 - selectedIndex;
                Sounds.ui_move1.Play();
                return;
            }

            if (Pressed(MenuButton.Start) || Pressed(MenuButton.Back))
            {
                Close();
                return;
            }

            if (Pressed(MenuButton.Confirm))
            {
                if (selectedIndex == 0)
                {
                    Close();
                }
                else
                {
                    Quit();
                }
            }
        }

        public override void Render()
        {
            base.Render();

            var centerX = 160f + (ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f);

            Draw.Rect(centerX - 52f, TOP - 9f, 104f, 60f, Color.Black * 0.7f);

            DrawCentered("PAUSE", new Vector2(centerX, TOP), Color.White);

            for (int index = 0; index < Options.Length; index++)
            {
                var selected = index == selectedIndex;
                var label = selected ? "> " + Options[index] + " <" : Options[index];

                DrawCentered(label, new Vector2(centerX, TOP + 14f + index * LINE_HEIGHT), selected ? Color.Gold : Color.LightGray);
            }

            DrawCentered("GAME KEEPS RUNNING", new Vector2(centerX, TOP + 14f + Options.Length * LINE_HEIGHT + 4f), Color.Gray);
        }

        private void Close()
        {
            IsOpen = false;
            Sounds.ui_unpause.Play();
            ServiceCollections.ResolveInputService().ResetPolledInput();
            RemoveSelf();
        }

        private void Quit()
        {
            quitRequested = true;
            Sounds.ui_clickBack.Play();

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            matchmakingService.RunOnGameThread(() =>
            {
                if (TFGame.Instance.Scene is not Level level)
                {
                    return;
                }

                ServiceCollections.ResolveReplayService().Export();
                ServiceCollections.ResolveNetplayManager().Reset();

                var lobby = matchmakingService.GetOwnLobby();
                if (!lobby.IsEmpty)
                {
                    Task.Run(async () => await matchmakingService.LeaveLobby(() => { }, () => { }));
                }

                level.GoToNetplayEntryMenu();

                var inputService = ServiceCollections.ResolveInputService();
                inputService.EnableAllControllers();
                inputService.RebindLocalInput();
            });
        }

        private static void DrawCentered(string text, Vector2 position, Color color)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    if (offsetX == 0 && offsetY == 0)
                    {
                        continue;
                    }

                    Draw.TextJustify(TFGame.Font, text, position + new Vector2(offsetX, offsetY), Color.Black, JustifyVec);
                }
            }

            Draw.TextJustify(TFGame.Font, text, position, color, JustifyVec);
        }

        private static bool Pressed(MenuButton button)
        {
            foreach (var input in TFGame.PlayerInputs)
            {
                if (input is KeyboardInput keyboard && KeyboardPressed(keyboard.Config, button))
                {
                    return true;
                }

                if (input is XGamepadInput pad && pad.Config != null && pad.XGamepad != null && PadPressed(pad, button))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool KeyboardPressed(KeyboardConfig config, MenuButton button)
        {
            if (config == null)
            {
                return false;
            }

            return button switch
            {
                MenuButton.Confirm => MInput.Keyboard.Pressed(config.Jump),
                MenuButton.Back => MInput.Keyboard.Pressed(config.Shoot, config.AltShoot),
                MenuButton.Up => MInput.Keyboard.Pressed(config.Up),
                MenuButton.Down => MInput.Keyboard.Pressed(config.Down),
                _ => MInput.Keyboard.Pressed(config.Start),
            };
        }

        private static bool PadPressed(XGamepadInput pad, MenuButton button)
        {
            return button switch
            {
                MenuButton.Confirm => PadButtonPressed(pad, pad.Config.Jump),
                MenuButton.Back => PadButtonPressed(pad, pad.Config.Shoot) || PadButtonPressed(pad, pad.Config.AltShoot),
                MenuButton.Up => PadButtonPressed(pad, pad.Config.Up) || pad.XGamepad.LeftStickUpPressed(0.5f),
                MenuButton.Down => PadButtonPressed(pad, pad.Config.Down) || pad.XGamepad.LeftStickDownPressed(0.5f),
                _ => PadButtonPressed(pad, pad.Config.Start),
            };
        }

        private static bool PadButtonPressed(XGamepadInput pad, Microsoft.Xna.Framework.Input.Buttons[] buttons)
        {
            return (bool)DynamicData.For(pad).Invoke("PressedButton", buttons);
        }
    }
}
