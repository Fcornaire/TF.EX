using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Moments.Encoder;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class GifExport
    {
        private const int Width = 320;
        private const int Height = 240;

        private const int MaxFrames = 200;
        private const int MinStride = 3;
        private const int Quality = 10;
        private const int Scale = 2;

        public enum Phase { Idle, Capturing, Encoding, Done, Failed }

        public static Phase State { get; private set; } = Phase.Idle;
        public static string Message { get; private set; }
        public static float Progress { get; private set; }

        public static bool IsCapturing => State == Phase.Capturing;
        public static bool IsBusy => State == Phase.Capturing || State == Phase.Encoding;

        private static readonly List<ReplayFrame> _frames = new List<ReplayFrame>();

        private static int _stride;
        private static int _delayMs;
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
        }

        public static string Begin(int inFrame, int outFrame, string replayName, string folder)
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
            _path = BuildPath(folder, replayName, inFrame, outFrame);
            _endFrame = outFrame;
            _seen = 0;
            _stallLimit = span * 3 + 600;

            _stride = Math.Max(MinStride, (int)Math.Ceiling(span / (float)MaxFrames));
            _delayMs = (int)Math.Round(_stride * 1000.0 / 60.0);

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
                if (_seen++ % _stride == 0 && _frames.Count < MaxFrames)
                {
                    var pixels = GrabScreen();

                    if (pixels == null)
                    {
                        return;
                    }

                    _frames.Add(new ReplayFrame
                    {
                        CPUData = pixels,
                        ScreenOffset = Vector2.Zero,
                        ScreenOffsetAdd = Vector2.Zero,
                    });
                }

                Progress = _frames.Count / (float)MaxFrames;

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
            var path = _path;
            var delay = _delayMs;

            new Thread(() => Encode(frames, path, delay))
            {
                IsBackground = true,
                Name = "tf-replay-gif",
            }.Start();
        }

        private static void Encode(ReplayFrame[] frames, string path, int delayMs)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var encoder = new GifEncoder(0, Quality);

                encoder.SetSize(Width, Height, Scale);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    encoder.Start(stream);

                    for (int i = 0; i < frames.Length; i++)
                    {
                        encoder.SetDelay(i == 0 ? 0 : delayMs);
                        encoder.AddFrame(frames[i]);

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
        }

        private static string BuildPath(string folder, string replayName, int inFrame, int outFrame)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = $"{Directory.GetCurrentDirectory()}\\Replays";
            }

            var stem = string.IsNullOrEmpty(replayName)
                ? "replay"
                : Path.GetFileNameWithoutExtension(replayName);

            return $"{folder}\\{stem}_{inFrame}-{outFrame}.gif";
        }
    }
}
