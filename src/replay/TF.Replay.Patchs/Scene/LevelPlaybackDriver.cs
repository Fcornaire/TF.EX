using HarmonyLib;
using Microsoft.Extensions.Logging;
using TF.Replay.Domain;
using TF.Replay.Domain.CustomComponent;
using TF.Replay.Domain.Extensions;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    /// <summary>
    /// TF.Replay's own playback driver, used when no host mod is driving frames
    /// </summary>
    [HarmonyPatch(typeof(Level))]
    internal static class LevelPlaybackDriver
    {
        private const int IntroWaitLimit = 900;

        private const int TrialsWaitLimit = 60;

        private static bool _exhaustedReported;
        private static bool _consuming;
        private static bool _priming;
        private static int _waited;
        private static int _generation;
        private static EndOfReplayPrompt _prompt;
        private static Level _outgoing;
        private static Level _driving;
        private static bool _divergenceReported;
        private static bool _captureFailed;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update(Level __instance)
        {
            if (!StandalonePlayback.IsActive)
            {
                Rearm();
                _driving = null;
                PlaybackInputs.CurrentFlat = null;
                return;
            }

            if (_generation != StandalonePlayback.Generation)
            {
                var watched = _driving;

                _generation = StandalonePlayback.Generation;
                Rearm();

                _outgoing = watched;
            }

            if (ReferenceEquals(__instance, _outgoing))
            {
                return;
            }

            _outgoing = null;
            _driving = __instance;

            if (__instance.Paused)
            {
                return;
            }

            if (!_consuming)
            {
                if (!ReadyToStart(__instance))
                {
                    return;
                }

                _consuming = true;
                _priming = true;
            }

            var service = ServiceCollections.ResolveReplayService();

            if (_priming)
            {
                _priming = false;

                if (!service.SeekTo(0))
                {
                    ServiceCollections.ResolveLogger()
                        .LogWarning("Could not restore the start of the replay; playback will drift");
                }
            }

            var record = service.ConsumeNextRecord();

            if (record == null)
            {
                PlaybackInputs.CurrentFlat = null;

                if (_prompt == null)
                {
                    __instance.Add(_prompt = new EndOfReplayPrompt());
                }

                if (!_exhaustedReported)
                {
                    _exhaustedReported = true;
                    ServiceCollections.ResolveLogger()
                        .LogDebug("Playback finished at frame {frame}", service.PlaybackFrame);
                }

                return;
            }

            if (_prompt != null)
            {
                if (_prompt.Scene != null)
                {
                    _prompt.RemoveSelf();
                }

                _prompt = null;
            }

            PlaybackInputs.CurrentFlat = service.GetRecordAt(record.Frame + 1)?.Inputs ?? record.Inputs;

            Verify(record);
        }

        private static void Verify(TF.Replay.Domain.Models.Record record)
        {
            var state = ServiceCollections.ResolveStateApi();

            if (state == null || record.State == null)
            {
                return;
            }

            byte[] live;

            try
            {
                state.SetCurrentFrame(record.Frame);

                live = state.GetGameStateBytesForRecording();
            }
            catch (Exception e)
            {
                if (!_captureFailed)
                {
                    _captureFailed = true;
                    ServiceCollections.ResolveLogger().LogError("Could not capture the live state at frame {frame}: {error}", record.Frame, e);
                }

                return;
            }

            if (live == null || live.AsSpan().SequenceEqual(record.State))
            {
                return;
            }

            if (_divergenceReported)
            {
                return;
            }

            _divergenceReported = true;

            var classified = state.ClassifyStateDiff(live, record.State);

            ServiceCollections.ResolveLogger().LogWarning("Playback diverged at frame {frame} ({kind}): {detail}",record.Frame, classified[0], classified[1]);
        }

        private static void Rearm()
        {
            _exhaustedReported = false;
            _consuming = false;
            _priming = false;
            _waited = 0;
            _prompt = null;
            _outgoing = null;
            _divergenceReported = false;
            _captureFailed = false;
        }

        private static bool ReadyToStart(Level level)
        {
            if (level.Get<Player>() == null)
            {
                return false;
            }

            var limit = level.Session?.MatchSettings?.Mode == Modes.Trials
                ? TrialsWaitLimit
                : IntroWaitLimit;

            return (level.Session?.RoundLogic?.RoundStarted ?? false) || _waited++ > limit;
        }
    }
}
