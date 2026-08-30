using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
{
    public class LoadingGauge : Entity
    {
        private const float CenterX = 160f;
        private const float CenterY = 120f;
        private const float BarWidth = 120f;
        private const float BarHeight = 4f;

        private readonly string _message;
        private int _done;
        private int _total;

        public LoadingGauge(string message)
        {
            _message = message?.ToUpperInvariant() ?? "";
            Depth = -10000;
        }

        public void Report(int done, int total)
        {
            _done = done;
            _total = total;
        }

        public override void Render()
        {
            base.Render();

            Draw.OutlineTextCentered(TFGame.Font, _message, new Vector2(CenterX, CenterY), Color.White, Color.Black);

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
