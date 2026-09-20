using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusPlayerCountButton : LobbyBorderButton
    {
        private const int MAX_PLAYERS = 4;
        private const int DEFAULT_PLAYERS = 2;

        private int MinPlayers => Math.Max(
            IsTeamMode ? LobbyVersusModeButton.TEAM_MODE_MIN_PLAYERS : 2,
            Context.LobbyBuilderContext.IsEditing ? ownLobby.Players.Count : 0);

        private bool IsTeamMode => (TowerFall.Modes)ownLobby.GameData.Mode == TowerFall.Modes.TeamDeathmatch;

        public LobbyVersusPlayerCountButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 34)
        {
            if (ownLobby.MaxPlayers < MinPlayers || ownLobby.MaxPlayers > MAX_PLAYERS)
            {
                ownLobby.MaxPlayers = Math.Max(DEFAULT_PLAYERS, MinPlayers);
            }

            UpdateSides(ownLobby.MaxPlayers);
        }

        public override void Update()
        {
            base.Update();

            if (ownLobby.IsSeriesLobby)
            {
                UpdateSeriesShape();
                return;
            }

            if (!base.Selected)
            {
                return;
            }

            if (MenuInput.Right && ownLobby.MaxPlayers < MAX_PLAYERS)
            {
                Sounds.ui_move2.Play();
                ownLobby.MaxPlayers++;
                base.OnConfirm();
                UpdateSides(ownLobby.MaxPlayers);
            }
            else if (MenuInput.Left && ownLobby.MaxPlayers > MinPlayers)
            {
                Sounds.ui_move2.Play();
                ownLobby.MaxPlayers--;
                base.OnConfirm();
                UpdateSides(ownLobby.MaxPlayers);
            }
            else
            {
                UpdateSides(ownLobby.MaxPlayers);
            }
        }

        private void UpdateSides(int maxPlayers)
        {
            DrawRight = maxPlayers < MAX_PLAYERS;
            DrawLeft = maxPlayers > MinPlayers;
        }

        private void UpdateSeriesShape()
        {
            var isLocked = Context.LobbyBuilderContext.IsEditing;
            var isTeams = ownLobby.MaxPlayers == LobbyVersusSeriesButton.TEAM_PLAYERS;

            DrawLeft = !isLocked && isTeams;
            DrawRight = !isLocked && !isTeams;

            if (isLocked || !base.Selected)
            {
                return;
            }

            if (MenuInput.Right && !isTeams || MenuInput.Left && isTeams)
            {
                Sounds.ui_move2.Play();
                ownLobby.MaxPlayers = isTeams ? LobbyVersusSeriesButton.DUEL_PLAYERS : LobbyVersusSeriesButton.TEAM_PLAYERS;
                LobbyVersusSeriesButton.EnforceSeriesRules(ownLobby);
                base.OnConfirm();
            }
        }

        public override void Render()
        {
            var detail = ownLobby.IsSeriesLobby
                ? $"{ownLobby.MaxPlayers} ARCHERS - {LobbyVersusSeriesButton.ShapeName(ownLobby)}"
                : $"{ownLobby.MaxPlayers} ARCHERS";

            Draw.OutlineTextCentered(TFGame.Font, "LOBBY SIZE", Position + new Vector2(0f, -6f), base.DrawColor, 2f);
            Draw.OutlineTextCentered(TFGame.Font, detail, Position + new Vector2(0f, 6f), base.DrawColor, 1f);

            base.Render();
        }
    }
}
