using Microsoft.Xna.Framework.Audio;
using TF.State.Domain.Context;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    internal class SFXService : ISFXService
    {
        private IStateContext _stateContext;

        public SFXService(IStateContext gameContext)
        {
            _stateContext = gameContext;
        }

        public void AddDesired(SFX toPlay)
        {
            _stateContext.AddesiredSfx(toPlay);
        }

        public void AddSoundEffect(SoundEffect data, string filename)
        {
            _stateContext.AddSoundEffect(data, filename);
        }

        public IEnumerable<SFX> Get()
        {
            return _stateContext.GetDesiredSfx().ToList();
        }

        private readonly Dictionary<int, List<SFX>> _desiredByFrame = new Dictionary<int, List<SFX>>();

        public void SaveSnapshot(int frame)
        {
            var desiredSfxs = _stateContext.GetDesiredSfx();

            var expired = desiredSfxs
                .Where(sfx => frame - sfx.Frame > Constants.SFX_STATE_LIFETIME)
                .ToList();

            foreach (var sfx in expired)
            {
                desiredSfxs.Remove(sfx);
            }

            _desiredByFrame[frame] = desiredSfxs.ToList();

            var cutoff = frame - Constants.SFX_SNAPSHOT_HISTORY;

            foreach (var stale in _desiredByFrame.Keys.Where(f => f < cutoff).ToList())
            {
                _desiredByFrame.Remove(stale);
            }
        }

        public void RestoreSnapshot(int frame)
        {
            if (_desiredByFrame.TryGetValue(frame, out var desired))
            {
                _stateContext.LoadDesiredSfx(desired.ToList());
            }
        }

        public void ClearSnapshots()
        {
            _desiredByFrame.Clear();
        }

        public void Load(IEnumerable<SFX> sFXes)
        {
            _stateContext.LoadDesiredSfx(sFXes);
        }

        public void Synchronize(int currentFrame, bool isTestMode)
        {
            RemoveFinishedSfx();

            var desiredSfxs = _stateContext.GetDesiredSfx();
            var currentSfxs = _stateContext.GetCurrentSfxs();
            var lastRollbackFrame = _stateContext.GetLastRollbackFrame();

            var unmatchedDesired = desiredSfxs.ToList();
            var unmatchedCurrent = currentSfxs.ToList();

            PairDesiredWithCurrent(unmatchedDesired, unmatchedCurrent);

            foreach (var sfx in unmatchedDesired.OrderBy(sfx => sfx.Frame))
            {
                if (sfx.Frame >= lastRollbackFrame && sfx.Frame <= currentFrame && sfx.Data != null)
                {
                    var toPlay = sfx.ToSoundEffectInstance();

                    currentSfxs.Add(new SoundEffectPlaying
                    {
                        Name = sfx.Name,
                        Frame = sfx.Frame,
                        SoundEffectInstance = toPlay,
                    });

                    toPlay.Play();
                }
            }

            if (lastRollbackFrame < currentFrame)
            {
                foreach (var sfx in unmatchedCurrent)
                {
                    if (currentFrame - sfx.Frame <= Constants.SFX_STATE_LIFETIME)
                    {
                        sfx.SoundEffectInstance.Stop();
                        sfx.SoundEffectInstance.Dispose();
                        currentSfxs.Remove(sfx);
                    }
                }
            }

            UpdateLastRollbackFrame(currentFrame);
        }

        public void UpdateLastRollbackFrame(int frame)
        {
            _stateContext.UpdateLastRollbackFrame(frame);
        }

        private static void PairDesiredWithCurrent(List<SFX> desired, List<SoundEffectPlaying> current)
        {
            var candidates = new List<(int Distance, SFX Desired, SoundEffectPlaying Current)>();

            foreach (var sfx in desired)
            {
                foreach (var playing in current)
                {
                    var distance = Math.Abs(playing.Frame - sfx.Frame);

                    if (playing.Name == sfx.Name && distance <= Constants.MAX_SFX_DELAY)
                    {
                        candidates.Add((distance, sfx, playing));
                    }
                }
            }

            foreach (var candidate in candidates.OrderBy(candidate => candidate.Distance))
            {
                if (desired.Contains(candidate.Desired) && current.Contains(candidate.Current))
                {
                    desired.Remove(candidate.Desired);
                    current.Remove(candidate.Current);
                }
            }
        }

        private void RemoveFinishedSfx()
        {
            var toRemove = new List<SoundEffectPlaying>();
            var currentSfxs = _stateContext.GetCurrentSfxs();

            foreach (var sfx in currentSfxs)
            {
                if (sfx.SoundEffectInstance.State != SoundState.Playing)
                {
                    sfx.SoundEffectInstance.Dispose();
                    toRemove.Add(sfx);
                }
            }

            foreach (var sfx in toRemove)
            {
                currentSfxs.Remove(sfx);
            }
        }

        public void Clear()
        {
            _stateContext.ClearDesiredSfx();
        }

        public void Reset()
        {
            _stateContext.ClearSfxs();
        }

        public string GetSoundEffectName(SoundEffect data)
        {
            return _stateContext.GetSoundEffectName(data);
        }
    }
}
