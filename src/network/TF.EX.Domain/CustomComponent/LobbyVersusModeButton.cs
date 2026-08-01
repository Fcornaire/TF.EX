using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusModeButton : LobbyBorderButton
    {
        public const int TEAM_MODE_PLAYERS = 4;

        private static readonly TowerFall.Modes[] Modes =
        {
            TowerFall.Modes.LastManStanding,
            TowerFall.Modes.HeadHunters,
            TowerFall.Modes.TeamDeathmatch,
        };

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

            if (!base.Selected)
            {
                return;
            }

            var index = IndexOfCurrentMode();

            if (MenuInput.Right && index < Modes.Length - 1)
            {
                SelectMode(Modes[index + 1]);
            }
            else if (MenuInput.Left && index > 0)
            {
                SelectMode(Modes[index - 1]);
            }
        }

        private void SelectMode(TowerFall.Modes mode)
        {
            Sounds.ui_move2.Play();
            ownLobby.GameData.Mode = (int)mode;

            if (mode == TowerFall.Modes.TeamDeathmatch)
            {
                ownLobby.MaxPlayers = TEAM_MODE_PLAYERS;
            }

            base.OnConfirm();
            UpdateSides();
        }

        private int IndexOfCurrentMode()
        {
            return Array.IndexOf(Modes, (TowerFall.Modes)ownLobby.GameData.Mode);
        }

        private void UpdateSides()
        {
            var index = IndexOfCurrentMode();

            DrawLeft = index > 0;
            DrawRight = index < Modes.Length - 1;
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
