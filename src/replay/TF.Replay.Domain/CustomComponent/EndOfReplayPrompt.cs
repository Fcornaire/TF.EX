using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class EndOfReplayPrompt : HUD
    {
        private const float PanelTop = 172f;
        private const float PanelWidth = 152f;
        private const float PanelHeight = 46f;

        private const float PanelCenterX = 160f;

        public EndOfReplayPrompt()
        {
            Depth = -10000;
        }

        public override void Render()
        {
            base.Render();

            var left = PanelCenterX - PanelWidth / 2f;

            Draw.Rect(left - 1f, PanelTop - 1f, PanelWidth + 2f, PanelHeight + 2f, Color.White * 0.5f);
            Draw.Rect(left, PanelTop, PanelWidth, PanelHeight, Color.Black * 0.9f);

            Draw.OutlineTextCentered(TFGame.Font, "THE REPLAY IS OVER",
                new Vector2(PanelCenterX, PanelTop + 12f), Color.Gold, Color.Black);

            Draw.OutlineTextCentered(TFGame.Font, "R : RESTART",
                new Vector2(PanelCenterX, PanelTop + 27f), Color.White, Color.Black);

            Draw.OutlineTextCentered(TFGame.Font, "ESC : QUIT TO MENU",
                new Vector2(PanelCenterX, PanelTop + 37f), Color.White, Color.Black);
        }
    }
}
