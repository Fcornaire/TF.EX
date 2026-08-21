using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class MatchEndChoices : Entity
    {
        private const int CHOICE_SECONDS = 30;
        private const int HUD_LAYER = 4;

        private const float MARGIN = 2f;
        private const float BASE_WIDTH = 320f;
        private const float WIDE_SAFETY = 4f;
        private const float TOP = 32f;
        private const float LINE_HEIGHT = 10f;

        private static readonly Vector2 LeftAligned = new Vector2(0f, 0.5f);

        private readonly DateTime deadline = DateTime.UtcNow.AddSeconds(CHOICE_SECONDS);

        private MatchEndChoices() : base(HUD_LAYER)
        {
            Position = Vector2.Zero;
            Depth = -2000000;
        }

        public static void Create(Level level)
        {
            var layer = level.Layers.Single(entry => entry.Key == HUD_LAYER).Value;

            if (layer.Entities.Any(entity => entity is MatchEndChoices))
            {
                return;
            }

            var choices = new MatchEndChoices();

            DynamicData.For(choices).Set("Scene", level);
            layer.Entities.Add(choices);
        }

        public override void Render()
        {
            base.Render();

            var statuses = ServiceCollections.ResolveMatchmakingService().GetEndGameStatus().ToList();

            if (statuses.Count == 0)
            {
                return;
            }

            var secondsLeft = Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds));
            var left = LeftEdge();

            DrawOutlined($"PICK IN {secondsLeft}", new Vector2(left, TOP), secondsLeft <= 5 ? Color.Red : Color.White);

            var y = TOP + LINE_HEIGHT;

            foreach (var status in statuses)
            {
                var label = string.IsNullOrEmpty(status.Choice) ? "..." : Describe(status.Choice);

                DrawOutlined($"P{status.Seat + 1} {label}", new Vector2(left, y), ArcherData.GetColorA(status.Seat));

                y += LINE_HEIGHT;
            }
        }

        private static float LeftEdge()
        {
            var width = Monocle.Engine.Instance?.Screen?.Width ?? (int)BASE_WIDTH;
            var offset = (width - BASE_WIDTH) / 2f;

            return offset > WIDE_SAFETY ? WIDE_SAFETY - offset : MARGIN;
        }

        private static void DrawOutlined(string text, Vector2 position, Color color)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    if (offsetX == 0 && offsetY == 0)
                    {
                        continue;
                    }

                    Draw.TextJustify(TFGame.Font, text, position + new Vector2(offsetX, offsetY), Color.Black, LeftAligned);
                }
            }

            Draw.TextJustify(TFGame.Font, text, position, color, LeftAligned);
        }

        private static string Describe(string choice)
        {
            return choice == "Rematch" ? "REMATCH" : "ARCHERS";
        }
    }
}
