using HarmonyLib;
using Microsoft.Extensions.Logging;
using Monocle;
using MonoMod.Utils;
using System.Diagnostics;
using TF.EX.Domain.Context;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain
{
    public static class InstantReplayFootage
    {
        private const int KeyframeStride = 60;
        private const int FootageFrames = ReplayRecorder.RECORD_FRAMES;
        private const float TickSeconds = 1f / Constants.NETPLAY_FPS;
        private const float FootageSeconds = FootageFrames * (ReplayRecorder.TIME_BETWEEN_FRAMES + TickSeconds);
        private const int HalfSpeedFootageTicks = (int)(FootageSeconds * Constants.NETPLAY_FPS * 2);
        private const int Goal = HalfSpeedFootageTicks + 2 * KeyframeStride + Constants.MAX_PREDICTION_TICKS;

        private static readonly Action<float> SetEngineTimeMult = StaticSetter<float>(nameof(Engine.TimeMult));
        private static readonly Action<float> SetEngineDeltaTime = StaticSetter<float>(nameof(Engine.DeltaTime));

        private static readonly Dictionary<int, Input[]> _inputs = [];
        private static readonly Dictionary<int, byte[]> _keyframes = [];
        private static readonly Dictionary<int, float> _timeRates = [];
        private static readonly ReplayFrame[] _pool = new ReplayFrame[FootageFrames];
        private static readonly List<Entity> _heldResults = [];
        private static readonly List<Action> _heldMusic = [];

        private static Level _level;
        private static bool _isShown;
        private static bool _isBaking;
        private static bool _isHolding;
        private static bool _isMusicHeld;
        private static bool _hasStartedLive;

        public static bool IsPaused { get; private set; }

        public static int PlayedThisMatch { get; private set; }

        public static bool UsesScreenRecorder => StateApi.Current.IsInstantReplayEnabled() && ServiceCollections.ResolveNetplayManager().IsSpectatorMode();

        public static void Track(Level level)
        {
            if (_isBaking || !ExFlags.IsCaptureActive || !StateApi.Current.IsInstantReplayEnabled())
            {
                return;
            }

            if (_level != level)
            {
                _level = level;
                _isShown = false;
                _isHolding = true;
                _isMusicHeld = false;
                _heldResults.Clear();
                _heldMusic.Clear();
                _inputs.Clear();
                _keyframes.Clear();
                _timeRates.Clear();
            }

            if (UsesScreenRecorder)
            {
                return;
            }

            var frame = (int)level.FrameCounter;
            _inputs[frame] = [.. ServiceCollections.ResolveInputService().GetCurrentInputs()];
            _timeRates[frame] = TFGame.TimeRate;

            if (frame % KeyframeStride == 0)
            {
                _keyframes[frame] = StateApi.Current.CaptureGameState();
                Prune(frame - Goal);
            }
        }

        public static void TryStart(Level level)
        {
            if (level == null || level != _level || _isShown || _isBaking || IsPaused || !StateApi.Current.IsInstantReplayEnabled())
            {
                return;
            }

            var resultsAge = StateApi.Current.GetRoundResultsAge();

            if (resultsAge < (UsesScreenRecorder ? 1 : Constants.MAX_PREDICTION_TICKS))
            {
                return;
            }

            _isShown = true;

            if (IsCatchingUp())
            {
                ReleaseResults();
                return;
            }

            var footage = UsesScreenRecorder ? level.ReplayRecorder?.Data : Bake(level, (int)level.FrameCounter - resultsAge);

            if (footage == null || !StateApi.Current.StartInstantReplay(footage))
            {
                ReleaseResults();
                return;
            }

            _hasStartedLive = IsLive();
            IsPaused = true;
            PlayedThisMatch++;
        }

        public static void ResetMatch()
        {
            PlayedThisMatch = 0;
            IsPaused = false;
            _level = null;
            _isHolding = false;
            _isMusicHeld = false;
            _heldResults.Clear();
            _heldMusic.Clear();
        }

        public static void HoldResults(Level level)
        {
            if (!_isHolding || level != _level)
            {
                return;
            }

            if (IsCatchingUp())
            {
                ReleaseResults();
                return;
            }

            foreach (var layer in level.Layers.Values)
            {
                foreach (var entity in layer.Entities)
                {
                    if (entity.Visible && (entity is VersusRoundResults || entity is VersusMatchResults || entity is HUDFade))
                    {
                        entity.Visible = false;
                        _heldResults.Add(entity);
                        _isMusicHeld = true;
                    }
                }
            }
        }

        public static bool HoldMusic(Action play)
        {
            if (!_isMusicHeld || TFGame.Instance?.Scene != _level)
            {
                return false;
            }

            _heldMusic.Add(play);
            return true;
        }

        private static void ReleaseResults()
        {
            _isHolding = false;
            _isMusicHeld = false;

            foreach (var entity in _heldResults)
            {
                if (entity.Scene != null)
                {
                    entity.Visible = true;
                }
            }

            _heldResults.Clear();

            var music = _heldMusic.ToList();
            _heldMusic.Clear();
            music.ForEach(play => play());
        }

        public static bool StepPause()
        {
            if (!IsPaused)
            {
                return false;
            }

            if (TFGame.Instance.Scene is not Level || !StateApi.Current.IsInstantReplayPlaying() || (IsLive() && !_hasStartedLive))
            {
                Resume();
                return false;
            }

            var deltaTime = (float)Engine.Instance.TargetElapsedTime.TotalSeconds;
            SetEngineDeltaTime?.Invoke(deltaTime);
            SetEngineTimeMult?.Invoke(deltaTime * Constants.VANILLA_FPS);

            StateApi.Current.TickInstantReplay();

            if (!StateApi.Current.IsInstantReplayPlaying())
            {
                Resume();
            }

            return true;
        }

        public static void Abort()
        {
            if (IsPaused)
            {
                Resume();
                return;
            }

            ReleaseResults();
        }

        private static bool IsCatchingUp()
        {
            return IsLive() && GGRSFFI.netplay_frames_behind() > Constants.SPECTATOR_CATCHUP_THRESHOLD;
        }

        private static bool IsLive()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            return netplayManager.IsSpectatorMode() && netplayManager.IsSpectatorCatchupEnabled();
        }

        private static void Resume()
        {
            IsPaused = false;
            StateApi.Current.StopInstantReplay();
            ReleaseResults();
        }

        private static ReplayData Bake(Level level, int end)
        {
            var logger = ServiceCollections.ResolveLogger();
            var start = KeyframeAtOrBefore(FootageStart(end), end);

            if (start < 0 || !HasInputs(start, end))
            {
                logger.LogWarning($"[InstantReplay] nothing to rebuild before frame {end}");
                return null;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();
            var dynScene = DynamicData.For(level as Monocle.Scene);
            var watch = Stopwatch.StartNew();

            var live = StateApi.Current.CaptureGameState();
            var liveInputs = inputService.GetCurrentInputs().ToArray();
            var liveFrameCounter = level.FrameCounter;
            var liveDeltaTime = Engine.DeltaTime;
            var liveTimeMult = Engine.TimeMult;
            var frames = new List<ReplayFrame>();

            ReleasePool();

            _isBaking = true;
            netplayManager.UpdateFramesToReSimulate(1);
            StateApi.Current.SetInstantReplayBaking(true);

            try
            {
                Restore(netplayManager, _keyframes[start]);
                ClearParticles(level);
                Audio.ClearLists();

                var elapsed = 0f;

                for (int frame = start; frame < end; frame++)
                {
                    inputService.UpdateCurrent(_inputs[frame]);
                    dynScene.Set("FrameCounter", (float)frame);
                    level.Update();

                    elapsed += Engine.DeltaTime;

                    if (elapsed >= ReplayRecorder.TIME_BETWEEN_FRAMES)
                    {
                        frames.Add(Capture(level, frames.Count % FootageFrames, elapsed, _inputs[frame]));
                        elapsed = 0f;
                    }
                }

                if (frames.Count > FootageFrames)
                {
                    frames.RemoveRange(0, frames.Count - FootageFrames);
                }
            }
            finally
            {
                SetEngineDeltaTime?.Invoke(liveDeltaTime);
                SetEngineTimeMult?.Invoke(liveTimeMult);
                Restore(netplayManager, live);
                ClearParticles(level);
                Audio.ClearLists();
                inputService.UpdateCurrent(liveInputs);
                dynScene.Set("FrameCounter", liveFrameCounter);
                StateApi.Current.SetInstantReplayBaking(false);
                netplayManager.UpdateFramesToReSimulate(0);
                _isBaking = false;
            }

            logger.LogInformation($"[InstantReplay] rebuilt {frames.Count} frames from {start} to {end} in {watch.ElapsedMilliseconds} ms");

            return frames.Count > 0 ? new ReplayData([.. frames]) : null;
        }

        private static ReplayFrame Capture(Level level, int index, float elapsed, Input[] inputs)
        {
            var device = Engine.Instance.GraphicsDevice;
            var screen = Engine.Instance.Screen.RenderTarget;

            level.PreRender();
            device.SetRenderTarget(screen);
            device.Clear(Engine.Instance.Screen.ClearColor);
            level.CoreRender(screen);
            device.SetRenderTarget(null);

            var frame = _pool[index] ??= new ReplayFrame();
            frame.Record(elapsed, 0L);
            frame.Input = new InputState[Math.Max(4, TFGame.Players.Length)];

            for (int seat = 0; seat < frame.Input.Length && seat < inputs.Length; seat++)
            {
                frame.Input[seat] = inputs[seat].ToTFInput();
            }

            Audio.ClearLists();

            return frame;
        }

        private static void ReleasePool()
        {
            for (int index = 0; index < _pool.Length; index++)
            {
                _pool[index]?.Dispose();
                _pool[index] = null;
            }
        }

        private static void Restore(Ports.INetplayManager netplayManager, byte[] state)
        {
            netplayManager.SetIsUpdating(true);

            try
            {
                StateApi.Current.RestoreGameStateBytes(state);
            }
            finally
            {
                netplayManager.SetIsUpdating(false);
            }
        }

        private static int FootageStart(int end)
        {
            var from = end;
            var gameTime = 0f;

            while (gameTime < FootageSeconds && from > end - Goal && _timeRates.TryGetValue(from - 1, out var timeRate))
            {
                from--;
                gameTime += TickSeconds * timeRate;
            }

            return from;
        }

        private static int KeyframeAtOrBefore(int from, int end)
        {
            var candidates = _keyframes.Keys.Where(frame => frame < end).ToList();

            if (candidates.Count == 0)
            {
                return -1;
            }

            var before = candidates.Where(frame => frame <= from).ToList();

            return before.Count > 0 ? before.Max() : candidates.Min();
        }

        private static bool HasInputs(int start, int end)
        {
            for (int frame = start; frame < end; frame++)
            {
                if (!_inputs.ContainsKey(frame))
                {
                    return false;
                }
            }

            return true;
        }

        private static void Prune(int oldest)
        {
            foreach (var frame in _inputs.Keys.Where(frame => frame < oldest).ToList())
            {
                _inputs.Remove(frame);
            }

            foreach (var frame in _keyframes.Keys.Where(frame => frame < oldest).ToList())
            {
                _keyframes.Remove(frame);
            }

            foreach (var frame in _timeRates.Keys.Where(frame => frame < oldest).ToList())
            {
                _timeRates.Remove(frame);
            }
        }

        private static void ClearParticles(Level level)
        {
            level.ParticlesBG?.Clear();
            level.Particles?.Clear();
            level.ParticlesFG?.Clear();
        }

        private static Action<T> StaticSetter<T>(string property)
        {
            var setter = AccessTools.PropertySetter(typeof(Engine), property);

            return setter?.CreateDelegate<Action<T>>();
        }
    }
}
