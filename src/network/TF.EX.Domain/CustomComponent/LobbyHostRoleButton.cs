using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyHostRoleButton : LobbyBorderButton
    {
        public LobbyHostRoleButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 34)
        {
            UpdateSides();
        }

        public override void Update()
        {
            base.Update();

            if (!base.Selected)
            {
                return;
            }

            if (MenuInput.Right && !Context.LobbyBuilderContext.IsSpectatorHost)
            {
                UpdateHost(true);
            }
            else if (MenuInput.Left && Context.LobbyBuilderContext.IsSpectatorHost)
            {
                UpdateHost(false);
            }
        }

        public override void Render()
        {
            var detail = Context.LobbyBuilderContext.IsSpectatorHost ? "SPECTATOR" : "PLAYER";

            Draw.OutlineTextCentered(TFGame.Font, "JOIN AS", Position + new Vector2(0f, -6f), base.DrawColor, 2f);
            Draw.OutlineTextCentered(TFGame.Font, detail, Position + new Vector2(0f, 6f), base.DrawColor, 1f);

            base.Render();
        }

        private void UpdateHost(bool isSpectatorHost)
        {
            Sounds.ui_move2.Play();
            Context.LobbyBuilderContext.IsSpectatorHost = isSpectatorHost;
            base.OnConfirm();
            UpdateSides();
        }

        private void UpdateSides()
        {
            DrawLeft = Context.LobbyBuilderContext.IsSpectatorHost;
            DrawRight = !Context.LobbyBuilderContext.IsSpectatorHost;
        }
    }
}
