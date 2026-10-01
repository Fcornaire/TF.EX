using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Moments.Encoder;
using TowerFall;

namespace TF.Replay.Domain
{
    public enum GifQuality { Vanilla, High }

    public static class GifExport
    {
        private const int Width = 320;
        private const int Height = 240;

        private const int MaxFrames = 300;

        private record Preset(int FrameRate, int Scale, int Quality);

        public enum Phase { Idle, Capturing, Encoding, Done, Failed }

        public static GifQuality Quality { get; set; } = GifQuality.High;

        public static Phase State { get; private set; } = Phase.Idle;
        public static string Message { get; private set; }
        public static float Progress { get; private set; }

        public static bool IsCapturing => State == Phase.Capturing;
        public static bool IsBusy => State == Phase.Capturing || State == Phase.Encoding;

        private static readonly List<Color[]> _frames = [];
        private static readonly List<int> _frameTicks = [];

        private static Preset _preset = HightPreset();
        private static int _stride;
        private static int _tickRate;
        private static int _nextTick;
        private static int _startFrame;
        private static int _endFrame;
        private static int _seen;
        private static int _stallLimit;
        private static string _path;
        private static Color[] _scratch;

        public static void Announce(string message)
        {
            if (IsBusy)
            {
                return;
            }

            Message = message;
            State = Phase.Failed;
        }

        public static void Reset()
        {
            if (State == Phase.Encoding)
            {
                return;
            }

            State = Phase.Idle;
            Message = null;
            Progress = 0f;
            _frames.Clear();
            _frameTicks.Clear();
        }

        public static string Begin(int inFrame, int outFrame, string replayName)
        {
            if (IsBusy)
            {
                return "EXPORT ALREADY RUNNING";
            }

            var span = outFrame - inFrame;

            if (span <= 0)
            {
                return "SELECT A RANGE FIRST";
            }

            _frames.Clear();
            _frameTicks.Clear();
            _path = BuildPath(replayName, inFrame, outFrame);
            _startFrame = inFrame;
            _endFrame = outFrame;
            _nextTick = inFrame;
            _seen = 0;
            _stallLimit = span * 3 + 600;

            _tickRate = ServiceCollections.ResolveReplayService()?.GetReplay()?.Informations?.TickRateOrLegacy ?? Models.ReplayInfo.LegacyTickRate;

            _preset = Quality == GifQuality.Vanilla ? VanillaPreset() : HightPreset();

            var minStride = (int)Math.Ceiling(_tickRate / (float)_preset.FrameRate);
            _stride = Math.Max(minStride, (int)Math.Ceiling(span / (float)MaxFrames));

            Progress = 0f;
            Message = "CAPTURING...";
            State = Phase.Capturing;

            return null;
        }

        public static void CaptureFrame(int playbackFrame)
        {
            if (State != Phase.Capturing)
            {
                return;
            }

            try
            {
                _seen++;

                if (playbackFrame >= _nextTick && _frames.Count < MaxFrames)
                {
                    var pixels = GrabScreen();

                    if (pixels == null)
                    {
                        return;
                    }

                    _frames.Add(pixels);
                    _frameTicks.Add(playbackFrame);
                    _nextTick = playbackFrame + _stride;
                }

                Progress = Math.Clamp((playbackFrame - _startFrame) / (float)(_endFrame - _startFrame), 0f, 1f);

                if (playbackFrame >= _endFrame || _frames.Count >= MaxFrames || _seen > _stallLimit)
                {
                    StartEncoding();
                }
            }
            catch (Exception e)
            {
                Fail($"CAPTURE FAILED", e);
            }
        }

        private static Color[] GrabScreen()
        {
            var game = TFGame.Instance;
            var screen = game?.Screen;

            if (screen == null)
            {
                return null;
            }

            var backBuffer = game.GraphicsDevice.PresentationParameters;
            var srcW = screen.ScaledWidth;
            var srcH = screen.ScaledHeight;
            var srcX = screen.DrawRect.X;
            var srcY = (backBuffer.BackBufferHeight - srcH) / 2;

            if (srcW <= 0 || srcH <= 0
                || srcX < 0 || srcY < 0
                || srcX + srcW > backBuffer.BackBufferWidth
                || srcY + srcH > backBuffer.BackBufferHeight)
            {
                return null;
            }

            if (_scratch == null || _scratch.Length != srcW * srcH)
            {
                _scratch = new Color[srcW * srcH];
            }

            game.GraphicsDevice.GetBackBufferData(new Rectangle(srcX, srcY, srcW, srcH), _scratch, 0, _scratch.Length);

            var pixels = new Color[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                var srcRow = Math.Min((int)((y + 0.5f) * srcH / Height), srcH - 1) * srcW;

                for (int x = 0; x < Width; x++)
                {
                    pixels[y * Width + x] = _scratch[srcRow + Math.Min((int)((x + 0.5f) * srcW / Width), srcW - 1)];
                }
            }

            return pixels;
        }

        private static void StartEncoding()
        {
            if (_frames.Count == 0)
            {
                Fail("NOTHING CAPTURED", null);
                return;
            }

            State = Phase.Encoding;
            Message = "ENCODING...";
            Progress = 0f;

            var frames = _frames.ToArray();
            var delays = FrameDelaysCentiSeconds(_frameTicks, _stride, _tickRate);
            var path = _path;
            var preset = _preset;

            _frames.Clear();
            _frameTicks.Clear();

            new Thread(() => Encode(frames, delays, path, preset))
            {
                IsBackground = true,
                Name = "tf-replay-gif",
            }.Start();
        }

        private static int[] FrameDelaysCentiSeconds(List<int> ticks, int stride, int tickRate)
        {
            var delays = new int[ticks.Count];
            var origin = ticks[0];

            for (int i = 0; i < ticks.Count; i++)
            {
                var start = ticks[i] - origin;
                var end = i + 1 < ticks.Count ? ticks[i + 1] - origin : start + stride;

                delays[i] = ToCentiseconds(end, tickRate) - ToCentiseconds(start, tickRate);
            }

            return delays;
        }

        private static int ToCentiseconds(int ticks, int tickRate) => (int)Math.Round(ticks * 100.0 / tickRate);

        private static Preset VanillaPreset()
        {
            GifExportOptions.Load();

            return new Preset(GifExportOptions.FrameRate, GifExportOptions.Scale, GifExportOptions.Quality);
        }

        private static Preset HightPreset() => new(FrameRate: 25, Scale: 2, Quality: 1);

        private static void Encode(Color[][] frames, int[] delaysCs, string path, Preset preset)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var encoder = new GifEncoder(0, preset.Quality);

                encoder.SetSize(Width, Height, preset.Scale);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    encoder.Start(stream);

                    for (int i = 0; i < frames.Length; i++)
                    {
                        encoder.SetDelay(delaysCs[i] * 10);
                        encoder.AddFrame(new ReplayFrame
                        {
                            CPUData = frames[i],
                            ScreenOffset = Vector2.Zero,
                            ScreenOffsetAdd = Vector2.Zero,
                        });
                        frames[i] = null;

                        Progress = (i + 1) / (float)frames.Length;
                    }

                    encoder.Finish();
                }

                Message = $"SAVED {Path.GetFileName(path)}".ToUpperInvariant();
                State = Phase.Done;
            }
            catch (Exception e)
            {
                Fail("ENCODE FAILED", e);
            }
        }

        private static void Fail(string message, Exception e)
        {
            ServiceCollections.ResolveLogger()?.LogError("Gif export failed: {message} {error}", message, e);

            Message = message;
            State = Phase.Failed;
            _frames.Clear();
            _frameTicks.Clear();
        }

        private static string BuildPath(string replayName, int inFrame, int outFrame)
        {
            var folder = Services.ReplayService.GifsRootFolder;
            Directory.CreateDirectory(folder);

            var stem = string.IsNullOrEmpty(replayName)
                ? "replay"
                : Path.GetFileNameWithoutExtension(replayName);

            return Path.Combine(folder, $"{stem}_{inFrame}-{outFrame}.gif");
        }
    }
}
