using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace TF.InputDisplayer.Domain
{
    public static class Letterbox
    {
        private static readonly AccessTools.FieldRef<Monocle.Screen, Rectangle> LeftPad = AccessTools.FieldRefAccess<Monocle.Screen, Rectangle>("leftPadDrawRect");

        private static readonly AccessTools.FieldRef<Monocle.Screen, Rectangle> RightPad = AccessTools.FieldRefAccess<Monocle.Screen, Rectangle>("rightPadDrawRect");

        private static readonly AccessTools.FieldRef<Monocle.Screen, Viewport> ScreenViewport = AccessTools.FieldRefAccess<Monocle.Screen, Viewport>("viewport");

        public readonly struct Region
        {
            public readonly float Width;
            public readonly float Anchor;
            public readonly float Height;
            public readonly float Scale;

            public Region(float width, float anchor, float height, float scale)
            {
                Width = width;
                Anchor = anchor;
                Height = height;
                Scale = scale;
            }

            public bool Exists => Width > 0f && Height > 0f && Scale > 0f;
        }

        public static Region Of(bool mirrored)
        {
            var screen = Engine.Instance?.Screen;

            if (screen == null)
            {
                return default;
            }

            var viewport = ScreenViewport(screen);
            var pad = mirrored ? RightPad(screen) : LeftPad(screen);

            var from = Math.Max(pad.X, 0);
            var to = Math.Min(pad.X + pad.Width, viewport.Width);

            return new Region(to - from, mirrored ? from : to, viewport.Height, screen.Scale);
        }
    }
}
