using Microsoft.Xna.Framework;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public abstract class RefusableModeButton(Vector2 position, Vector2 tweenFrom, string title, string subtitle) : MainModeButton(position, tweenFrom, title, subtitle)
    {
        private const int REFUSAL_DURATION = 300;

        public Func<string> RefusalCheck { get; set; }

        protected override void OnConfirm()
        {
            var refusal = RefusalCheck?.Invoke();

            if (refusal != null)
            {
                Refuse(MainMenu, refusal);
                return;
            }

            base.OnConfirm();
        }

        public static void Refuse(MainMenu menu, string refusal)
        {
            Sounds.ui_invalid.Play();
            Notification.Create(menu, refusal, 10, REFUSAL_DURATION);

            foreach (var button in menu.GetAll<RefusableModeButton>().Where(button => button.Confirmed).ToList())
            {
                button.Deconfirm();
                button.Selected = true;
            }
        }
    }
}
