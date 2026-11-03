using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Interop;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusModeButton : LobbyBorderButton
    {
        public const int TEAM_MODE_MIN_PLAYERS = 3;

        private string[] AvailableModes => ownLobby.IsSeriesLobby
            ? new[] { ownLobby.MaxPlayers == LobbyVersusSeriesButton.TEAM_PLAYERS ? NetplayGameModes.TeamDeathmatch : NetplayGameModes.LastManStanding }
            : NetplayGameModes.All.Select(mode => mode.Id).ToArray();

        public LobbyVersusModeButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 200, 30)
        {
            if (IndexOfCurrentMode() < 0)
            {
                ownLobby.GameData.Mode = AvailableModes[0];
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
                ownLobby.GameData.Mode = modes[0];
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

        private void SelectMode(string id)
        {
            Sounds.ui_move2.Play();
            ownLobby.GameData.Mode = id;

            var minimumPlayers = NetplayGameModes.Resolve(id)?.MinimumPlayers ?? 2;

            if (ownLobby.MaxPlayers < minimumPlayers)
            {
                ownLobby.MaxPlayers = minimumPlayers;
            }

            base.OnConfirm();
            UpdateSides();
        }

        private int IndexOfCurrentMode()
        {
            return Array.IndexOf(AvailableModes, ownLobby.GameData.Mode);
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

            var mode = NetplayGameModes.Resolve(ownLobby.GameData.Mode) ?? NetplayGameModes.Resolve(NetplayGameModes.LastManStanding);

            Draw.OutlineTextureCentered(mode.Icon, Position + new Vector2(0f, -20f), Color.White, new Vector2(1f + iconWiggler.Value * 0.1f, 1f - iconWiggler.Value * 0.1f));
            Draw.OutlineTextCentered(TFGame.Font, "GAME MODE:", Position + new Vector2(0f, -5f), base.DrawColor, 1f);
            Draw.OutlineTextCentered(TFGame.Font, mode.Name.ToUpperInvariant(), Position + new Vector2(0f, 4f), base.DrawColor, 2f);
        }
    }
}
