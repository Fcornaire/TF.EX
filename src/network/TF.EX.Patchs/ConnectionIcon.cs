using Microsoft.Xna.Framework;
using Monocle;

namespace TF.EX.Patchs
{
    public static class ConnectionIcon
    {
        public const float Size = 7f;

        private static readonly string[] Direct =
        {
            "#######",
            "#.#.#.#",
            "#.#.#.#",
            "#######",
            "#######",
            ".#####.",
            "..###..",
        };

        private static readonly string[] Relay =
        {
            "#######",
            "#####.#",
            "#######",
            ".......",
            "#######",
            "#####.#",
            "#######",
        };

        public static float OutlineAlpha(float alpha) => MathF.Pow(alpha, 4f);

        public static void Render(Vector2 topLeft, bool isRelayed, Color color, float alpha = 1f)
        {
            var rows = isRelayed ? Relay : Direct;

            foreach (var (x, y) in Pixels(rows))
            {
                Draw.Rect(topLeft.X + x - 1f, topLeft.Y + y - 1f, 3f, 3f, Color.Black * OutlineAlpha(alpha));
            }

            foreach (var (x, y) in Pixels(rows))
            {
                Draw.Rect(topLeft.X + x, topLeft.Y + y, 1f, 1f, color * alpha);
            }
        }

        private static IEnumerable<(int X, int Y)> Pixels(string[] rows)
        {
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == '#')
                    {
                        yield return (x, y);
                    }
                }
            }
        }
    }
}
