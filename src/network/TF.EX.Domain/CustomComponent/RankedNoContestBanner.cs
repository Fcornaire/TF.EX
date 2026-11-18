using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class RankedNoContestBanner : Entity
    {
        private const int HUD_LAYER = 4;
        private const float TOP = 3f;
        private const float CENTER_X = 160f;

        private static readonly Color Gold = Calc.HexToColor("FFDC6B");
        private static readonly Vector2 Centered = new(0.5f, 0f);

        private RankedNoContestBanner() : base(HUD_LAYER)
        {
            Position = Vector2.Zero;
            Depth = -2000000;
        }

        public static void Create(Level level)
        {
            var layer = level.Layers.Single(entry => entry.Key == HUD_LAYER).Value;

            if (layer.Entities.Any(entity => entity is RankedNoContestBanner))
            {
                return;
            }

            var banner = new RankedNoContestBanner();

            DynamicData.For(banner).Set("Scene", level);
            layer.Entities.Add(banner);
        }

        public override void Render()
        {
            base.Render();

            var matchmakingService = ServiceCollections.ResolveMatchmakingService();

            if (matchmakingService.GetRankedNoContestChoice() != RankedNoContestChoice.Accept)
            {
                return;
            }

            var text = $"NO CONTEST OFFERED ({matchmakingService.GetRankedNoContestSecondsLeft()}) - PAUSE TO ACCEPT";

            Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(CENTER_X, TOP), Gold, Color.Black, Centered);
        }
    }
}
