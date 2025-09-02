using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusModeButton : LobbyBorderButton
    {
        public LobbyVersusModeButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 200, 30)
        {
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
