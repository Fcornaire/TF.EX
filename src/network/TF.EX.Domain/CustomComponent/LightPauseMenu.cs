using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TowerFall;
using static TF.EX.Domain.CustomComponent.RawMenuInput;

namespace TF.EX.Domain.CustomComponent
{
    public class LightPauseMenu : Entity
    {
        private enum PendingConfirm { None, Forfeit, OfferNoContest, AcceptNoContest }

        private const int HUD_LAYER = 4;
        private const float TOP = 96f;
        private const float LINE_HEIGHT = 12f;
        private const float MIN_WIDTH = 104f;
        private const float SIDE_MARGIN = 16f;
        private const int NO_CONTEST_INDEX = 1;
        private const string FORFEIT_LABEL = "FORFEIT";

        private const string RESUME_LABEL = "RESUME";

        private static readonly string[] Options = { RESUME_LABEL, "QUIT" };
        private static readonly Vector2 JustifyVec = new(0.5f, 0.5f);

        private int selectedIndex;
        private bool quitRequested;
        private bool skipOpeningFrame = true;
        private ConfirmDialog dialog;

        private static LightPauseMenu live;
        private static PendingConfirm pendingConfirm;

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

            if (level.Session.GetWinner() != -1 || !IsButtonPressed(RawMenuButton.Start))
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
            pendingConfirm = PendingConfirm.None;
            live = null;
        }

        private static void Attach(Level level)
        {
            live = new LightPauseMenu();
            level.Add(live);

            if (pendingConfirm != PendingConfirm.None)
            {
                live.ShowDialog(level, pendingConfirm);
            }
        }

        public override void Removed()
        {
            base.Removed();
            dialog?.Close();

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

            if (dialog != null)
            {
                UpdateDialog();
                return;
            }

            var step = IsButtonPressed(RawMenuButton.Down) ? 1 : IsButtonPressed(RawMenuButton.Up) ? -1 : 0;

            if (step != 0)
            {
                var count = CurrentOptions().Length;

                selectedIndex = (selectedIndex + step + count) % count;
                Sounds.ui_move1.Play();
                return;
            }

            if (IsButtonPressed(RawMenuButton.Start) || IsButtonPressed(RawMenuButton.Back))
            {
                Close();
                return;
            }

            if (IsButtonPressed(RawMenuButton.Confirm))
            {
                if (selectedIndex == 0)
                {
                    Close();
                }
                else if (!IsRankedPlayer())
                {
                    Quit();
                }
                else if (selectedIndex == NO_CONTEST_INDEX)
                {
                    ChooseNoContest();
                }
                else
                {
                    Sounds.ui_click.Play();
                    ShowDialog((Level)Scene, PendingConfirm.Forfeit);
                }
            }
        }

        public override void Render()
        {
            base.Render();

            if (dialog != null)
            {
                return;
            }

            const float centerX = 160f;

            var options = CurrentOptions();
            var width = Math.Max(MIN_WIDTH, options.Max(option => TFGame.Font.MeasureString($"> {option} <").X) + SIDE_MARGIN);
            var height = 60f + (options.Length - 2) * LINE_HEIGHT;
            var isNoContestGreyed = IsRankedPlayer() && !IsNoContestAvailable();

            Draw.Rect(centerX - width / 2f, TOP - 9f, width, height, Color.Black * 0.7f);

            DrawCentered("PAUSE", new Vector2(centerX, TOP), Color.White);

            for (int index = 0; index < options.Length; index++)
            {
                var selected = index == selectedIndex;
                var label = selected ? "> " + options[index] + " <" : options[index];
                var color = isNoContestGreyed && index == NO_CONTEST_INDEX ? Color.Gray : selected ? Color.Gold : Color.LightGray;

                DrawCentered(label, new Vector2(centerX, TOP + 14f + index * LINE_HEIGHT), color);
            }

            DrawCentered("GAME KEEPS RUNNING", new Vector2(centerX, TOP + 14f + options.Length * LINE_HEIGHT + 4f), Color.Gray);
        }

        private void Close()
        {
            CloseDialog();
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

        private static string[] CurrentOptions()
        {
            return IsRankedPlayer() ? [RESUME_LABEL, GetNoContestLabel(), FORFEIT_LABEL] : Options;
        }

        private static string GetNoContestLabel()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            return matchmakingService.GetRankedNoContestChoice() switch
            {
                RankedNoContestChoice.Offer => "OFFER NO CONTEST",
                RankedNoContestChoice.Offered => "NO CONTEST OFFERED",
                RankedNoContestChoice.Accept => $"ACCEPT NO CONTEST ({matchmakingService.GetRankedNoContestSecondsLeft()})",
                _ => "NO CONTEST",
            };
        }

        private static bool IsNoContestAvailable()
        {
            var choice = ServiceCollections.ResolveMatchmakingService().GetRankedNoContestChoice();

            return choice == RankedNoContestChoice.Offer || choice == RankedNoContestChoice.Accept;
        }

        private void ChooseNoContest()
        {
            var choice = ServiceCollections.ResolveMatchmakingService().GetRankedNoContestChoice() switch
            {
                RankedNoContestChoice.Offer => PendingConfirm.OfferNoContest,
                RankedNoContestChoice.Accept => PendingConfirm.AcceptNoContest,
                _ => PendingConfirm.None,
            };

            if (choice == PendingConfirm.None)
            {
                Sounds.ui_invalid.Play();
                return;
            }

            Sounds.ui_click.Play();
            ShowDialog((Level)Scene, choice);
        }

        private void ShowDialog(Level level, PendingConfirm kind)
        {
            pendingConfirm = kind;
            dialog = kind switch
            {
                PendingConfirm.OfferNoContest => ConfirmDialog.OpenOverLevelLayer("OFFER NO CONTEST?", "UNRATED IF ACCEPTED", "OFFER"),
                PendingConfirm.AcceptNoContest => ConfirmDialog.OpenOverLevelLayer("ACCEPT NO CONTEST?", AcceptDetail(), "ACCEPT"),
                _ => ConfirmDialog.OpenOverLevelLayer($"{FORFEIT_LABEL}?", "YOU LOSE THE SERIES", FORFEIT_LABEL),
            };

            level.Add(dialog);
        }

        private void UpdateDialog()
        {
            if (IsDialogStale())
            {
                Sounds.ui_invalid.Play();
                CloseDialog();
                return;
            }

            if (dialog.IsCancelled)
            {
                Sounds.ui_clickBack.Play();
                CloseDialog();
                return;
            }

            if (pendingConfirm == PendingConfirm.AcceptNoContest)
            {
                dialog.SetDetail(AcceptDetail());
            }

            if (!dialog.IsConfirmed)
            {
                return;
            }

            var confirmed = pendingConfirm;
            CloseDialog();

            switch (confirmed)
            {
                case PendingConfirm.Forfeit:
                    Quit();
                    break;
                case PendingConfirm.OfferNoContest:
                    OfferNoContest();
                    break;
                default:
                    AcceptNoContest();
                    break;
            }
        }

        private static bool IsDialogStale()
        {
            var choice = ServiceCollections.ResolveMatchmakingService().GetRankedNoContestChoice();

            return pendingConfirm switch
            {
                PendingConfirm.OfferNoContest => choice != RankedNoContestChoice.Offer,
                PendingConfirm.AcceptNoContest => choice != RankedNoContestChoice.Accept,
                _ => false,
            };
        }

        private static string AcceptDetail()
        {
            return $"SERIES UNRATED ({ServiceCollections.ResolveMatchmakingService().GetRankedNoContestSecondsLeft()})";
        }

        private void OfferNoContest()
        {
            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (matchmakingService.TryOfferRankedNoContest())
            {
                matchmakingService.RunOnGameThread(() => Notification.Create(TFGame.Instance.Scene, "NO CONTEST OFFERED TO YOUR OPPONENT", 10, 300));
            }
            else
            {
                Sounds.ui_invalid.Play();
            }

            Close();
        }

        private void AcceptNoContest()
        {
            ServiceCollections.ResolveMatchmakingService().AcceptRankedNoContest();
            Close();
        }

        private void CloseDialog()
        {
            pendingConfirm = PendingConfirm.None;
            dialog?.Close();
            dialog = null;
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
