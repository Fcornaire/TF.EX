using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Interop;
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

        // A mod mode plays as the built-in mode it is closest to (GameData.Mode) and is named by GameData.CustomMode
        private (TowerFall.Modes Mode, string CustomMode)[] AvailableModes => ownLobby.IsSeriesLobby
            ? new[] { (ownLobby.MaxPlayers == LobbyVersusSeriesButton.TEAM_PLAYERS ? TowerFall.Modes.TeamDeathmatch : TowerFall.Modes.LastManStanding, "") }
            : Modes.Select(mode => (mode, ""))
                .Concat(NetplayVersusModes.Ids.Select(NetplayVersusModes.Resolve).Where(entry => entry != null)
                    .Select(entry => (entry.VersusGameMode.IsTeamMode ? TowerFall.Modes.TeamDeathmatch : TowerFall.Modes.LastManStanding, entry.Name)))
                .ToArray();

        public LobbyVersusModeButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 200, 30)
        {
            if (IndexOfCurrentMode() < 0)
            {
                ownLobby.GameData.Mode = (int)Modes[0];
                ownLobby.GameData.CustomMode = "";
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
                ownLobby.GameData.Mode = (int)modes[0].Mode;
                ownLobby.GameData.CustomMode = modes[0].CustomMode;
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

        private void SelectMode((TowerFall.Modes Mode, string CustomMode) option)
        {
            var mode = option.Mode;

            Sounds.ui_move2.Play();
            ownLobby.GameData.Mode = (int)mode;
            ownLobby.GameData.CustomMode = option.CustomMode;

            var teamMin = NetplayVersusModes.TeamModeMinPlayers(ownLobby, TEAM_MODE_MIN_PLAYERS);

            if (mode == TowerFall.Modes.TeamDeathmatch && ownLobby.MaxPlayers < teamMin)
            {
                ownLobby.MaxPlayers = teamMin;
            }

            base.OnConfirm();
            UpdateSides();
        }

        private int IndexOfCurrentMode()
        {
            return Array.IndexOf(AvailableModes, ((TowerFall.Modes)ownLobby.GameData.Mode, ownLobby.GameData.CustomMode));
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
            var customMode = NetplayVersusModes.Resolve(ownLobby.GameData.CustomMode)?.VersusGameMode;

            Draw.OutlineTextureCentered(customMode?.Icon.Subtexture ?? VersusModeButton.GetModeIcon(mode), Position + new Vector2(0f, -20f), Color.White, new Vector2(1f + iconWiggler.Value * 0.1f, 1f - iconWiggler.Value * 0.1f));
            Draw.OutlineTextCentered(TFGame.Font, "GAME MODE:", Position + new Vector2(0f, -5f), base.DrawColor, 1f);
            Draw.OutlineTextCentered(TFGame.Font, customMode?.Name.ToUpperInvariant() ?? VersusModeButton.GetModeName(mode), Position + new Vector2(0f, 4f), base.DrawColor, 2f);
        }
    }
}
