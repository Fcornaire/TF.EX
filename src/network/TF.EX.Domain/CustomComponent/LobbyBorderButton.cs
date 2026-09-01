using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public abstract class LobbyBorderButton : BorderButton
    {
        protected Wiggler iconWiggler;

        protected Lobby ownLobby => ServiceCollections.ResolveMatchmakingService().GetOwnLobby();

        protected bool hasTweenedUICamera = false;
        public LobbyBorderButton(Vector2 position, Vector2 tweenFrom, int width, int height) : base(position, tweenFrom, width, height)
        {
            iconWiggler = Wiggler.Create(15, 6f);
        }

        public override void Update()
        {
            base.Update();

            if (Selected && !hasTweenedUICamera)
            {
                hasTweenedUICamera = true;
                MainMenu.TweenUICameraToY(Math.Max(0f, base.Y - 200f));
            }

            if (!Selected && hasTweenedUICamera)
            {
                hasTweenedUICamera = false;
            }
        }
    }
}
