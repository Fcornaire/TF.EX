using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusPlayerCountButton : LobbyBorderButton
    {
        private const int MAX_PLAYERS = 4;

        private int MinPlayers => IsTeamMode ? LobbyVersusModeButton.TEAM_MODE_PLAYERS : 2;

        private bool IsTeamMode => (TowerFall.Modes)ownLobby.GameData.Mode == TowerFall.Modes.TeamDeathmatch;

        public LobbyVersusPlayerCountButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 34)
        {
            if (ownLobby.MaxPlayers < MinPlayers || ownLobby.MaxPlayers > MAX_PLAYERS)
            {
                ownLobby.MaxPlayers = MAX_PLAYERS;
            }

            UpdateSides(ownLobby.MaxPlayers);
        }

        public override void Update()
        {
            base.Update();

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

        public override void Render()
        {
            Draw.OutlineTextCentered(TFGame.Font, "LOBBY SIZE", Position + new Vector2(0f, -6f), base.DrawColor, 2f);
            Draw.OutlineTextCentered(TFGame.Font, $"{ownLobby.MaxPlayers} ARCHERS", Position + new Vector2(0f, 6f), base.DrawColor, 1f);

            base.Render();
        }
    }
}
