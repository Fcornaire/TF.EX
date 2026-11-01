using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TowerFall;
using static TF.EX.Domain.CustomComponent.RawMenuInput;

namespace TF.EX.Domain.CustomComponent
{
    public class LightPauseMenu : Entity
    {
        private const int HUD_LAYER = 4;
        private const float TOP = 96f;
        private const float LINE_HEIGHT = 12f;
        private const string FORFEIT_LABEL = "FORFEIT";

        private static readonly string[] Options = { "RESUME", "QUIT" };
        private static readonly string[] RankedOptions = { "RESUME", FORFEIT_LABEL };
        private static readonly Vector2 JustifyVec = new(0.5f, 0.5f);

        private int selectedIndex;
        private bool quitRequested;
        private bool skipOpeningFrame = true;
        private ConfirmDialog forfeitDialog;

        private static LightPauseMenu live;
        private static bool isForfeitDialogOpen;

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

            if (level.Ending || level.Session.GetWinner() != -1 || !Pressed(RawMenuButton.Start))
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
            isForfeitDialogOpen = false;
            live = null;
        }

        private static void Attach(Level level)
        {
            live = new LightPauseMenu();
            level.Add(live);

            if (isForfeitDialogOpen)
            {
                live.ShowForfeitDialog(level);
            }
        }

        public override void Removed()
        {
            base.Removed();
            forfeitDialog?.Close();

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

            if (forfeitDialog != null)
            {
                UpdateForfeitDialog();
                return;
            }

            if (Pressed(RawMenuButton.Up) || Pressed(RawMenuButton.Down))
            {
                selectedIndex = 1 - selectedIndex;
                Sounds.ui_move1.Play();
                return;
            }

            if (Pressed(RawMenuButton.Start) || Pressed(RawMenuButton.Back))
            {
                Close();
                return;
            }

            if (Pressed(RawMenuButton.Confirm))
            {
                if (selectedIndex == 0)
                {
                    Close();
                }
                else if (IsRankedPlayer())
                {
                    Sounds.ui_click.Play();
                    ShowForfeitDialog(Scene);
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

            if (forfeitDialog != null)
            {
                return;
            }

            var centerX = 160f + (ServiceCollections.ResolveWiderSetModApi()?.UIXOffset ?? 0f);

            Draw.Rect(centerX - 52f, TOP - 9f, 104f, 60f, Color.Black * 0.7f);

            DrawCentered("PAUSE", new Vector2(centerX, TOP), Color.White);

            var options = IsRankedPlayer() ? RankedOptions : Options;

            for (int index = 0; index < options.Length; index++)
            {
                var selected = index == selectedIndex;
                var label = selected ? "> " + options[index] + " <" : options[index];

                DrawCentered(label, new Vector2(centerX, TOP + 14f + index * LINE_HEIGHT), selected ? Color.Gold : Color.LightGray);
            }

            DrawCentered("GAME KEEPS RUNNING", new Vector2(centerX, TOP + 14f + options.Length * LINE_HEIGHT + 4f), Color.Gray);
        }

        private void Close()
        {
            CloseForfeitDialog();
            IsOpen = false;
            Sounds.ui_unpause.Play();
            ServiceCollections.ResolveInputService().ResetPolledInput();
            RemoveSelf();
        }

        private static bool IsRankedPlayer()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            return matchmakingService.GetOwnLobby().IsRanked && !matchmakingService.IsSpectator();
        }

        private void ShowForfeitDialog(Scene scene)
        {
            isForfeitDialogOpen = true;
            forfeitDialog = ConfirmDialog.OpenOverLevelLayer($"{FORFEIT_LABEL}?", "YOU LOSE THE SERIES", FORFEIT_LABEL);
            scene.Add(forfeitDialog);
        }

        private void UpdateForfeitDialog()
        {
            if (forfeitDialog.IsCancelled)
            {
                Sounds.ui_clickBack.Play();
                CloseForfeitDialog();
            }
            else if (forfeitDialog.IsConfirmed)
            {
                CloseForfeitDialog();
                Quit();
            }
        }

        private void CloseForfeitDialog()
        {
            isForfeitDialogOpen = false;
            forfeitDialog?.Close();
            forfeitDialog = null;
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
                inputService.ResetPolledInput();
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
    }
}
