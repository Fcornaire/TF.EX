using Microsoft.Xna.Framework.Audio;
using System.Collections.Immutable;
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

        public void Load(IEnumerable<SFX> sFXes)
        {
            _stateContext.LoadDesiredSfx(sFXes);
        }

        public void Synchronize(int currentFrame, bool isTestMode)
        {
            RemoveFinishedSfx(isTestMode);
            SyncCurrentToDesired(currentFrame);
            if (_stateContext.GetLastRollbackFrame() < currentFrame)
            {
                RemoveNotDesiredSfx(currentFrame);
            }
            UpdateLastRollbackFrame(currentFrame);
        }

        public void UpdateLastRollbackFrame(int frame)
        {
            _stateContext.UpdateLastRollbackFrame(frame);
        }

        private void RemoveNotDesiredSfx(int currentFrame)
        {
            var desiredSfxs = _stateContext.GetDesiredSfx();
            var currentSfxs = _stateContext.GetCurrentSfxs();
            var notPresent = currentSfxs
                .Where(sfx => desiredSfxs.All(desired => desired.Name != sfx.Name)).ToList();

            foreach (var sfx in notPresent)
            {
                sfx.SoundEffectInstance.Stop();
                sfx.SoundEffectInstance.Dispose();
                currentSfxs.Remove(sfx);
            }
        }

        private void RemoveFinishedSfx(bool isTestMode)
        {
            var toRemove = new List<SoundEffectPlaying>();
            var desiredSfxs = _stateContext.GetDesiredSfx();
            var currentSfxs = _stateContext.GetCurrentSfxs();

            foreach (var sfx in _stateContext.GetCurrentSfxs())
            {
                if (sfx.SoundEffectInstance.State != SoundState.Playing)
                {
                    sfx.SoundEffectInstance.Dispose();
                    toRemove.Add(sfx);

                    if (!isTestMode) //Hack test session synchronized, we don't care too much about sfx 
                    {
                        var toDel = desiredSfxs.SingleOrDefault(des => des.Name == sfx.Name && des.Frame == sfx.Frame);
                        if (toDel != null)
                        {
                            desiredSfxs.Remove(toDel);
                        }
                    }
                }
            }

            foreach (var sfx in toRemove)
            {
                currentSfxs.Remove(sfx);
            }
        }

        private void SyncCurrentToDesired(int currentFrame) //TODO: deal with same sfx at same frame
        {
            var desiredSfxs = _stateContext.GetDesiredSfx();
            var currentSfxs = _stateContext.GetCurrentSfxs();

            desiredSfxs
                .ToList()
                .ForEach(sfx =>
                {
                    if (!IsSfxSynchedToCurrent(sfx) && sfx.Frame <= currentFrame && sfx.Frame >= _stateContext.GetLastRollbackFrame() && sfx.Data != null)
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
                });
        }

        /// <summary>
        /// Check if the sfx is already been played
        /// </summary>
        private bool IsSfxSynchedToCurrent(SFX sfx)
        {
            var current = _stateContext.GetCurrentSfxs().Where(curr => curr.Name == sfx.Name).ToImmutableSortedDictionary(sfx => sfx.Frame, sfx => sfx).LastOrDefault().Value;

            if (current == null)
            {
                return false;
            }

            return current.Frame + Constants.MAX_SFX_DELAY >= sfx.Frame;
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
