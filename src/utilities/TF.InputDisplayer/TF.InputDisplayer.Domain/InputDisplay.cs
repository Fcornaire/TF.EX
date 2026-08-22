using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.InputDisplayer.Domain.Models;
using TowerFall;

namespace TF.InputDisplayer.Domain
{
    public static class InputDisplay
    {
        private const float TopY = 42f;
        private const float BottomY = 202f;
        private const float RowHeight = 14f;
        private const float BlockGap = 6f;
        private const float EdgeMargin = 2f;
        private const float InnerGap = 2f;
        private const float AccentWidth = 2f;
        private const float OutsideMargin = 4f;
        private const int OutsideButtonSlots = 2;

        private const int InsideMaxRows = 8;

        private const int MaxFramesShown = 999;

        private const double SlideSeconds = 0.07;

        public const int MaxRowsPerColumn = InputHistory.RowCapacity;

        private static readonly InputRow[][] _buffers = { new InputRow[InputHistory.RowCapacity], new InputRow[InputHistory.RowCapacity] };
        private static readonly int[] _counts = new int[2];
        private static readonly int[] _seats = new int[2];
        private static readonly int[] _held = new int[InputPacker.ButtonCount];

        private static readonly long[] _slideFrom = new long[InputHistory.MaxSeats];
        private static readonly int[] _newestRun = new int[InputHistory.MaxSeats];

        private static float _countWidth;
        private static int _frame;

        public static void RenderInside(InputHistory history, int frame)
        {
            Render(history, frame, outside: false);
        }

        public static void RenderOutside(InputHistory history, int frame)
        {
            Render(history, frame, outside: true);
        }

        private static void Render(InputHistory history, int frame, bool outside)
        {
            if (history == null || !DisplayOptions.Enabled || !InputIcons.Ready())
            {
                return;
            }

            EnsureMetrics();

            _frame = frame;

            RenderSide(history, frame, 0, 2, mirrored: false, outside);
            RenderSide(history, frame, 1, 3, mirrored: true, outside);
        }

        private static void RenderSide(InputHistory history, int frame, int first, int second, bool mirrored, bool outside)
        {
            var region = Region(mirrored, out var outsideScale);

            if (outside != (outsideScale >= 1))
            {
                return;
            }

            var columns = Collect(history, frame, first, second);

            if (columns == 0)
            {
                return;
            }

            if (outside)
            {
                DrawOutside(region, outsideScale, columns, mirrored);
                return;
            }

            var width = 0f;

            for (int i = 0; i < columns; i++)
            {
                width = Math.Max(width, ColumnWidth(WidestRow(_buffers[i], _counts[i])));
            }

            DrawInside(columns, width, mirrored);
        }

        private static Letterbox.Region Region(bool mirrored, out int outsideScale)
        {
            outsideScale = 0;

            if ((ScreenBounds.WideOffset?.Invoke() ?? 0f) > 0f)
            {
                return default;
            }

            var region = Letterbox.Of(mirrored);

            if (!region.Exists)
            {
                return region;
            }

            outsideScale = Math.Min((int)(region.Width / OutsideWidth()), (int)region.Scale);

            return region;
        }

        private static int Collect(InputHistory history, int frame, int first, int second)
        {
            var columns = 0;

            for (int i = 0; i < 2; i++)
            {
                var seat = i == 0 ? first : second;

                if (!Shows(seat))
                {
                    continue;
                }

                var count = history.Rows(seat, frame, _buffers[columns], DisplayOptions.MaxRows);

                if (count == 0)
                {
                    continue;
                }

                _seats[columns] = seat;
                _counts[columns] = count;
                columns++;
            }

            return columns;
        }

        private static void DrawInside(int columns, float width, bool mirrored)
        {
            var x = mirrored ? ScreenBounds.Right - width : ScreenBounds.Left;

            if (columns == 1)
            {
                DrawColumn(0, x, width, mirrored, TopY, TopY + InsideMaxRows * RowHeight);
                return;
            }

            var half = (BottomY - TopY - BlockGap) / 2f;

            DrawColumn(0, x, width, mirrored, TopY, TopY + half);
            DrawColumn(1, x, width, mirrored, TopY + half + BlockGap, BottomY);
        }

        private static void DrawOutside(Letterbox.Region region, int scale, int columns, bool mirrored)
        {
            var width = 0f;

            for (int i = 0; i < columns; i++)
            {
                width = Math.Max(width, ColumnWidth(WidestRow(_buffers[i], _counts[i])));
            }

            width = Math.Min(Math.Max(width, ColumnWidth(0)), region.Width / scale);

            var origin = mirrored ? region.Anchor : region.Anchor - width * scale;
            var height = region.Height / scale;

            var layout = !mirrored;

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                                   DepthStencilState.None, RasterizerState.CullNone, null,
                                   Matrix.CreateScale(scale) * Matrix.CreateTranslation(origin, 0f, 0f));

            if (columns == 1)
            {
                DrawColumn(0, 0f, width, layout, OutsideMargin, height - OutsideMargin);
            }
            else
            {
                var half = (height - OutsideMargin * 2f - BlockGap) / 2f;

                DrawColumn(0, 0f, width, layout, OutsideMargin, OutsideMargin + half);
                DrawColumn(1, 0f, width, layout, OutsideMargin + half + BlockGap, height - OutsideMargin);
            }

            Draw.SpriteBatch.End();
        }

        private static bool Shows(int seat) => seat < TFGame.Players.Length && TFGame.Players[seat];

        private static void DrawColumn(int column, float x, float width, bool mirrored, float top, float bottom)
        {
            var count = Math.Min(_counts[column], (int)((bottom - top) / RowHeight));

            if (count <= 0)
            {
                return;
            }

            var rows = _buffers[column];
            var seat = _seats[column];

            var height = count * RowHeight + 4f;
            var opacity = DisplayOptions.Opacity;
            var accent = ArcherData.GetColorA(seat);
            var slide = Slide(seat, _frame - (rows[0].Frames - 1));

            Draw.Rect(x, top - 2f, width, height, Color.Black * (0.45f * opacity));
            Draw.Rect(mirrored ? x + width - AccentWidth : x, top - 2f, AccentWidth, height, accent * opacity);

            var directionAt = EdgeMargin + AccentWidth + _countWidth + InnerGap;
            var buttonsAt = directionAt + InputIcons.Width + InnerGap;
            var buttonWidth = InputIcons.ButtonWidth;

            for (int i = 0; i < count; i++)
            {
                var row = rows[i];
                var y = top + (i - slide) * RowHeight;
                var middle = y + InputIcons.Height / 2f;
                var alpha = Fade(i, count) * opacity;

                if (i == 0)
                {
                    alpha *= 1f - slide;
                }

                var text = row.Frames > MaxFramesShown ? MaxFramesShown.ToString() : row.Frames.ToString();
                var textX = Offset(x, width, mirrored, EdgeMargin + AccentWidth, _countWidth) + (mirrored ? 0f : _countWidth);

                Draw.OutlineTextJustify(TFGame.Font, text, new Vector2(textX, middle), accent * alpha, Color.Black * alpha, new Vector2(mirrored ? 0f : 1f, 0.5f));

                Draw.Texture(InputIcons.Direction(row.Packed), new Vector2(Offset(x, width, mirrored, directionAt, InputIcons.Width), y), Color.White * alpha);

                var held = Held(row.Packed);

                for (int slot = 0; slot < held; slot++)
                {
                    var at = Offset(x, width, mirrored, buttonsAt + slot * buttonWidth, buttonWidth);

                    Draw.TextureCentered(InputIcons.Button(_held[slot]), new Vector2(at + buttonWidth / 2f, middle), Color.White * alpha);
                }
            }
        }

        private static int Held(int packed)
        {
            var held = 0;

            for (int button = 0; button < InputPacker.ButtonCount; button++)
            {
                if (InputPacker.Held(packed, InputPacker.Button(button)))
                {
                    _held[held++] = button;
                }
            }

            return held;
        }

        private static int WidestRow(InputRow[] rows, int count)
        {
            var widest = 0;

            for (int i = 0; i < count; i++)
            {
                widest = Math.Max(widest, Held(rows[i].Packed));
            }

            return widest;
        }

        private static void EnsureMetrics()
        {
            if (_countWidth <= 0f)
            {
                _countWidth = TFGame.Font != null
                    ? MathF.Ceiling(TFGame.Font.MeasureString(MaxFramesShown.ToString()).X)
                    : 15f;
            }
        }

        private static float OutsideWidth() => ColumnWidth(OutsideButtonSlots);

        private static float ColumnWidth(int buttons)
            => EdgeMargin + AccentWidth + _countWidth + InnerGap + InputIcons.Width + InnerGap
               + Math.Max(buttons, 1) * InputIcons.ButtonWidth + EdgeMargin;

        private static float Offset(float x, float width, bool mirrored, float offset, float itemWidth) => mirrored ? x + width - offset - itemWidth : x + offset;

        private static float Slide(int seat, int startedAt)
        {
            if (seat < 0 || seat >= _newestRun.Length)
            {
                return 0f;
            }

            if (_newestRun[seat] != startedAt)
            {
                _newestRun[seat] = startedAt;
                _slideFrom[seat] = startedAt == _frame ? Stopwatch.GetTimestamp() : 0L;
            }

            if (_slideFrom[seat] == 0L)
            {
                return 0f;
            }

            var elapsed = (Stopwatch.GetTimestamp() - _slideFrom[seat]) / (double)Stopwatch.Frequency;

            if (elapsed >= SlideSeconds)
            {
                _slideFrom[seat] = 0L;

                return 0f;
            }

            return 1f - Ease.CubeOut((float)(elapsed / SlideSeconds));
        }

        private static float Fade(int index, int count)
        {
            if (count <= 1)
            {
                return 1f;
            }

            var t = index / (float)(count - 1);

            return MathHelper.Lerp(1f, 0.4f, Math.Clamp((t - 0.35f) / 0.65f, 0f, 1f));
        }
    }
}
