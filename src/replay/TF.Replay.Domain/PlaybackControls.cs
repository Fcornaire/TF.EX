using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class PlaybackControls
    {
        private static readonly MethodInfo _mInputUpdate = typeof(MInput).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Static);

        public static bool IsPaused { get; private set; }
        public static int? HoverFrame { get; private set; }
        public static Vector2? MousePosition { get; private set; }
        public static int? MarkIn { get; private set; }
        public static int? MarkOut { get; private set; }
        public static bool ShowHelp { get; private set; }
        public static bool ShowHurtboxes { get; private set; }

        private static bool _stepQueued;
        private static bool _pausedBeforeHelp;
        private static int? _lastSeekedFrame;
        private static bool _seekRefusalShown;

        public static void Reset()
        {
            IsPaused = false;
            _stepQueued = false;
            HoverFrame = null;
            MousePosition = null;
            MarkIn = null;
            MarkOut = null;
            ShowHelp = false;
            ShowHurtboxes = false;
            _lastSeekedFrame = null;
            _pausedBeforeHelp = false;
            _seekRefusalShown = false;
            GifExport.Reset();
            ControlsHelp.Reset();
        }

        private static void MarkAt(int frame)
        {
            GifExport.Reset();

            if (MarkIn == null || MarkOut != null)
            {
                MarkIn = frame;
                MarkOut = null;
                return;
            }

            if (frame <= MarkIn.Value)
            {
                MarkIn = frame;
                return;
            }

            MarkOut = frame;
        }

        private static Vector2 GameMousePosition()
        {
            var game = TFGame.Instance;
            var screen = game?.Screen;

            if (screen == null || screen.Scale <= 0f)
            {
                return Vector2.Zero;
            }

            var raw = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var backBuffer = game.GraphicsDevice.PresentationParameters;
            var client = game.Window.ClientBounds;

            var toBackBufferX = client.Width > 0 ? backBuffer.BackBufferWidth / (float)client.Width : 1f;
            var toBackBufferY = client.Height > 0 ? backBuffer.BackBufferHeight / (float)client.Height : 1f;

            var letterboxY = (backBuffer.BackBufferHeight - screen.ScaledHeight) / 2f;

            return new Vector2(
                (raw.X * toBackBufferX - screen.DrawRect.X) / screen.Scale,
                (raw.Y * toBackBufferY - letterboxY - screen.DrawRect.Y) / screen.Scale);
        }

        public static bool ShouldRunUpdate()
        {
            _mInputUpdate?.Invoke(null, null);

            var service = ServiceCollections.ResolveReplayService();

            if (GifExport.IsCapturing)
            {
                HoverFrame = null;
                MousePosition = null;
                return true;
            }

            HandleMouse(service);

            if (MInput.Keyboard.Pressed(Keys.Escape))
            {
                QuitToBrowser();

                return true;
            }

            if (MInput.Keyboard.Pressed(Keys.R))
            {
                RestartReplay(service);

                return true;
            }

            if (MInput.Keyboard.Pressed(Keys.H))
            {
                ShowHelp = !ShowHelp;

                if (ShowHelp)
                {
                    _pausedBeforeHelp = IsPaused;
                    IsPaused = true;
                }
                else
                {
                    IsPaused = _pausedBeforeHelp;
                }
            }

            if (MInput.Keyboard.Pressed(Keys.F1))
            {
                ShowHurtboxes = !ShowHurtboxes;
            }

            if (MInput.Keyboard.Pressed(Keys.G))
            {
                StartGifExport(service);
            }

            if (MInput.Keyboard.Pressed(Keys.Space))
            {
                IsPaused = !IsPaused;
                _stepQueued = false;
            }

            if (MInput.Keyboard.Pressed(Keys.Left))
            {
                IsPaused = true;
                Seek(service, StepBackTarget(service));
            }

            if (!IsPaused)
            {
                return true;
            }

            if (MInput.Keyboard.Pressed(Keys.Right) || MInput.Keyboard.Check(Keys.Down))
            {
                _stepQueued = true;
            }

            if (!_stepQueued)
            {
                return false;
            }

            _stepQueued = false;
            return true;
        }

        private static void QuitToBrowser()
        {
            var level = TFGame.Instance?.Scene as Level;

            Sounds.ui_clickBack.Play();
            ServiceCollections.ResolveApi()?.StopPlayback();

            TFGame.Instance.Scene = new TowerFall.MainMenu(TowerFall.MainMenu.MenuState.Main);

            level?.Session?.MatchSettings?.LevelSystem?.Dispose();
        }

        private static void RestartReplay(Ports.IReplayService service)
        {
            var name = service?.GetReplay()?.Informations?.Name;

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var song = Music.CurrentSong;

            Music.Stop();
            Sounds.ui_mapZoom.Play();

            if (TFGame.Instance?.Scene is Level level)
            {
                level.Frozen = true;
            }

            ServiceCollections.ResolveApi()?.StopPlayback();

            var failure = ServiceCollections.LaunchReplay(name, song);

            if (failure != null)
            {
                ServiceCollections.Notify($"CANNOT RESTART REPLAY: {failure}".ToUpperInvariant());
                QuitToBrowser();

                return;
            }

            ServiceCollections.EnsureFakeControllers();
        }

        private static void StartGifExport(Ports.IReplayService service)
        {
            if (service == null || MarkIn == null || MarkOut == null)
            {
                GifExport.Announce("SELECT A RANGE FIRST (RIGHT-CLICK TWICE)");
                return;
            }

            if (SeekingIsOff(service))
            {
                return;
            }

            Seek(service, MarkIn.Value);

            var refusal = GifExport.Begin(MarkIn.Value, MarkOut.Value,
                service.GetReplay()?.Informations?.Name, service.CurrentFolder);

            if (refusal != null)
            {
                GifExport.Announce(refusal);
                return;
            }

            IsPaused = false;
        }

        private static void HandleMouse(Ports.IReplayService service)
        {
            HoverFrame = null;
            MousePosition = null;

            if (service == null || service.LastFrame <= 0)
            {
                return;
            }

            if (service.SeekBlockedBy != null)
            {
                MousePosition = GameMousePosition();
                return;
            }

            var mouse = GameMousePosition();

            MousePosition = mouse;

            if (!SeekBar.Contains(mouse))
            {
                return;
            }

            HoverFrame = SeekBar.FrameAt(mouse.X, service.LastFrame);

            if (MInput.Mouse.RightPressed)
            {
                MarkAt(HoverFrame.Value);
            }

            if (!MInput.Mouse.LeftCheck)
            {
                _lastSeekedFrame = null;
                return;
            }

            if (_lastSeekedFrame == HoverFrame.Value)
            {
                return;
            }

            _lastSeekedFrame = HoverFrame.Value;
            Seek(service, HoverFrame.Value);
        }


        private static int StepBackTarget(Ports.IReplayService service) => service.PlaybackFrame - (StandalonePlayback.IsActive ? 2 : 1);


        private static bool SeekingIsOff(Ports.IReplayService service)
        {
            var blockedBy = service?.SeekBlockedBy;

            if (blockedBy == null)
            {
                return false;
            }

            if (!_seekRefusalShown)
            {
                _seekRefusalShown = true;
                ServiceCollections.Notify($"NO SEEKING: {blockedBy} SAVES NO STATE".ToUpperInvariant());
            }

            Sounds.ui_invalid.Play();

            return true;
        }

        private static void Seek(Ports.IReplayService service, int frame)
        {
            if (service == null || TFGame.Instance?.Scene is not Level || SeekingIsOff(service))
            {
                return;
            }

            try
            {
                if (!service.SeekTo(frame))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                ServiceCollections.ResolveLogger()?.LogError("Could not seek to frame {frame}: {error}", frame, e);
                return;
            }

            var landed = service.PlaybackFrame;

            PlaybackInputs.CurrentFlat =
                service.GetRecordAt(landed + 1)?.Inputs ?? service.GetRecordAt(landed)?.Inputs;

            if (StandalonePlayback.IsActive)
            {
                service.ConsumeNextRecord();
            }
        }
    }
}
