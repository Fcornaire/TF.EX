using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class TrialsExportPrompt : HUD
    {
        private const float GuideY = 228f;

        private const float GuideX = 310f;

        private bool _taken;

        public TrialsExportPrompt()
        {
            Depth = -10000;
        }

        public override void Update()
        {
            base.Update();

            if (_taken || !SavePressed())
            {
                return;
            }

            _taken = true;

            var path = StandaloneRecorder.ExportNow();

            Sounds.ui_clickSpecial.Play();
            ServiceCollections.Notify(path != null ? "REPLAY SAVED" : "COULD NOT SAVE THE REPLAY");

            RemoveSelf();
        }

        public override void Render()
        {
            base.Render();

            if (_taken)
            {
                return;
            }

            var title = "SAVE REPLAY";
            var icon = Icon();
            var titleWidth = TFGame.Font.MeasureString(title).X;

            if (icon == null)
            {
                Draw.OutlineTextCentered(TFGame.Font, title,
                    new Vector2(GuideX - titleWidth / 2f, GuideY), Color.White, Color.Black);
                return;
            }

            var total = icon.Width + 4f + titleWidth;

            Draw.TextureCentered(icon, new Vector2(GuideX - icon.Width / 2f, GuideY), Color.White);
            Draw.OutlineTextCentered(TFGame.Font, title,
                new Vector2(GuideX - total + titleWidth / 2f, GuideY), Color.White, Color.Black);
        }

        private static bool SavePressed()
        {
            for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
            {
                if (TFGame.PlayerInputs[i] != null && TFGame.PlayerInputs[i].MenuAlt2)
                {
                    return true;
                }
            }

            return false;
        }

        private static Subtexture Icon()
        {
            for (int i = 0; i < TFGame.PlayerInputs.Length; i++)
            {
                if (TFGame.PlayerInputs[i] != null)
                {
                    return TFGame.PlayerInputs[i].Alt2Icon;
                }
            }

            return null;
        }
    }
}
