using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class UpdaterDialog : Entity
    {
        private const float CenterX = 160f;
        private const float CenterY = 120f;
        private const float BarWidth = 120f;
        private const float BarHeight = 4f;

        private readonly string _title;
        private string _phase = "";
        private long _done;
        private long _total;

        public UpdaterDialog(string title)
        {
            _title = title?.ToUpperInvariant() ?? "";
            Depth = -100000;
        }

        public void SetPhase(string phase)
        {
            _phase = phase?.ToUpperInvariant() ?? "";
            _done = 0;
            _total = 0;
        }

        public void Report(long done, long total)
        {
            _done = done;
            _total = total;
        }

        public override void Render()
        {
            base.Render();

            Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.7f);

            Draw.OutlineTextCentered(TFGame.Font, _title, new Vector2(CenterX, CenterY - 14f), Color.Gold, Color.Black);
            Draw.OutlineTextCentered(TFGame.Font, _phase, new Vector2(CenterX, CenterY), Color.White, Color.Black);

            var total = _total;

            if (total <= 0)
            {
                return;
            }

            var progress = Math.Min(1f, _done / (float)total);
            var left = CenterX - BarWidth / 2f;
            var top = CenterY + 10f;

            Draw.Rect(left - 1f, top - 1f, BarWidth + 2f, BarHeight + 2f, Color.White * 0.5f);
            Draw.Rect(left, top, BarWidth, BarHeight, Color.Black * 0.9f);
            Draw.Rect(left, top, BarWidth * progress, BarHeight, Color.Gold);
        }
    }
}
