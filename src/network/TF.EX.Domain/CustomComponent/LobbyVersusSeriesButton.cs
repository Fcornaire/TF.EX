using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusSeriesButton : LobbyBorderButton
    {
        public const int DUEL_PLAYERS = 2;
        public const int TEAM_PLAYERS = 4;

        private static readonly int[] Options = { 0, 1, 3, 5, 7 };

        private static bool IsLocked => Context.LobbyBuilderContext.IsEditing;

        public LobbyVersusSeriesButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 34)
        {
            if (IndexOfCurrent() < 0)
            {
                ownLobby.GameData.BestOf = 0;
            }

            UpdateSides();
        }

        public override void Update()
        {
            base.Update();

            if (!base.Selected || IsLocked)
            {
                return;
            }

            var index = IndexOfCurrent();

            if (MenuInput.Right && index < Options.Length - 1)
            {
                Select(Options[index + 1]);
            }
            else if (MenuInput.Left && index > 0)
            {
                Select(Options[index - 1]);
            }
        }

        public override void Render()
        {
            var bestOf = ownLobby.GameData.BestOf;
            var detail = bestOf == 0 ?
                    IsLocked ?
                        "OFF (SET AT CREATION ONLY)"
                        : "OFF"
                    : $"BEST OF {bestOf} - {ShapeName(ownLobby)}";

            Draw.OutlineTextCentered(TFGame.Font, "SERIES", Position + new Vector2(0f, -6f), base.DrawColor, 2f);
            Draw.OutlineTextCentered(TFGame.Font, detail, Position + new Vector2(0f, 6f), base.DrawColor, 1f);

            base.Render();
        }

        public static string ShapeName(Lobby lobby)
        {
            return lobby.MaxPlayers == TEAM_PLAYERS ? "2V2" : "1V1";
        }

        public static void EnforceSeriesRules(Lobby lobby)
        {
            if (!lobby.IsSeriesLobby)
            {
                return;
            }

            if (lobby.MaxPlayers != TEAM_PLAYERS)
            {
                lobby.MaxPlayers = DUEL_PLAYERS;
            }

            lobby.GameData.MapId = -1;
            lobby.GameData.Mode = (int)(lobby.MaxPlayers == TEAM_PLAYERS ? TowerFall.Modes.TeamDeathmatch : TowerFall.Modes.LastManStanding);

            MainMenu.VersusMatchSettings?.Variants.TournamentRules();
        }

        private void Select(int bestOf)
        {
            Sounds.ui_move2.Play();
            ownLobby.GameData.BestOf = bestOf;
            EnforceSeriesRules(ownLobby);
            base.OnConfirm();
            UpdateSides();
        }

        private int IndexOfCurrent()
        {
            return Array.IndexOf(Options, ownLobby.GameData.BestOf);
        }

        private void UpdateSides()
        {
            var index = IndexOfCurrent();

            DrawLeft = !IsLocked && index > 0;
            DrawRight = !IsLocked && index < Options.Length - 1;
        }
    }
}
