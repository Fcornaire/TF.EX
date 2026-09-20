using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusModeButton : LobbyBorderButton
    {
        public const int TEAM_MODE_MIN_PLAYERS = 3;

        private static readonly TowerFall.Modes[] Modes =
        {
            TowerFall.Modes.LastManStanding,
            TowerFall.Modes.HeadHunters,
            TowerFall.Modes.TeamDeathmatch,
        };

        private TowerFall.Modes[] AvailableModes => ownLobby.IsSeriesLobby
            ? new[] { ownLobby.MaxPlayers == LobbyVersusSeriesButton.TEAM_PLAYERS ? TowerFall.Modes.TeamDeathmatch : TowerFall.Modes.LastManStanding }
            : Modes;

        public LobbyVersusModeButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 200, 30)
        {
            if (IndexOfCurrentMode() < 0)
            {
                ownLobby.GameData.Mode = (int)Modes[0];
            }

            UpdateSides();
        }

        public override void Update()
        {
            base.Update();

            var modes = AvailableModes;
            var index = IndexOfCurrentMode();

            if (index < 0)
            {
                ownLobby.GameData.Mode = (int)modes[0];
                UpdateSides();
                return;
            }

            if (!base.Selected)
            {
                return;
            }

            if (MenuInput.Right && index < modes.Length - 1)
            {
                SelectMode(modes[index + 1]);
            }
            else if (MenuInput.Left && index > 0)
            {
                SelectMode(modes[index - 1]);
            }
        }

        private void SelectMode(TowerFall.Modes mode)
        {
            Sounds.ui_move2.Play();
            ownLobby.GameData.Mode = (int)mode;

            if (mode == TowerFall.Modes.TeamDeathmatch && ownLobby.MaxPlayers < TEAM_MODE_MIN_PLAYERS)
            {
                ownLobby.MaxPlayers = TEAM_MODE_MIN_PLAYERS;
            }

            base.OnConfirm();
            UpdateSides();
        }

        private int IndexOfCurrentMode()
        {
            return Array.IndexOf(AvailableModes, (TowerFall.Modes)ownLobby.GameData.Mode);
        }

        private void UpdateSides()
        {
            var index = IndexOfCurrentMode();

            DrawLeft = index > 0;
            DrawRight = index < AvailableModes.Length - 1;
        }

        public override void Render()
        {
            base.Render();

            var mode = (TowerFall.Modes)ownLobby.GameData.Mode;

            Draw.OutlineTextureCentered(VersusModeButton.GetModeIcon(mode), Position + new Vector2(0f, -20f), Color.White, new Vector2(1f + iconWiggler.Value * 0.1f, 1f - iconWiggler.Value * 0.1f));
            Draw.OutlineTextCentered(TFGame.Font, "GAME MODE:", Position + new Vector2(0f, -5f), base.DrawColor, 1f);
            Draw.OutlineTextCentered(TFGame.Font, VersusModeButton.GetModeName(mode), Position + new Vector2(0f, 4f), base.DrawColor, 2f);
        }
    }
}
