using Microsoft.Xna.Framework.Audio;
using System.Collections.Immutable;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    internal class SFXService : ISFXService
    {
        private IGameContext _gameContext;

        public SFXService(IGameContext gameContext)
        {
            _gameContext = gameContext;
        }

        public void AddDesired(SFX toPlay)
        {
            _gameContext.AddesiredSfx(toPlay);
        }

        public void AddSoundEffect(SoundEffect data, string filename)
        {
            _gameContext.AddSoundEffect(data, filename);
        }

        public IEnumerable<SFX> Get()
        {
            return _gameContext.GetDesiredSfx().ToList();
        }

        public void Load(IEnumerable<SFX> sFXes)
        {
            _gameContext.LoadDesiredSfx(sFXes);
        }

        public void Synchronize(int currentFrame, bool isTestMode)
        {
            RemoveFinishedSfx(isTestMode);
            SyncCurrentToDesired(currentFrame);
            if (_gameContext.GetLastRollbackFrame() < currentFrame)
            {
                RemoveNotDesiredSfx(currentFrame);
            }
            UpdateLastRollbackFrame(currentFrame);
        }

        public void UpdateLastRollbackFrame(int frame)
        {
            _gameContext.UpdateLastRollbackFrame(frame);
        }

        private void RemoveNotDesiredSfx(int currentFrame)
        {
            var desiredSfxs = _gameContext.GetDesiredSfx();
            var currentSfxs = _gameContext.GetCurrentSfxs();
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
            var desiredSfxs = _gameContext.GetDesiredSfx();
            var currentSfxs = _gameContext.GetCurrentSfxs();

            foreach (var sfx in _gameContext.GetCurrentSfxs())
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
            var desiredSfxs = _gameContext.GetDesiredSfx();
            var currentSfxs = _gameContext.GetCurrentSfxs();

            desiredSfxs
                .ToList()
                .ForEach(sfx =>
                {
                    if (!IsSfxSynchedToCurrent(sfx) && sfx.Frame <= currentFrame && sfx.Frame >= _gameContext.GetLastRollbackFrame())
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
        /// <param name="sfx"></param>
        /// <returns></returns>
        private bool IsSfxSynchedToCurrent(SFX sfx)
        {
            var current = _gameContext.GetCurrentSfxs().Where(curr => curr.Name == sfx.Name).ToImmutableSortedDictionary(sfx => sfx.Frame, sfx => sfx).LastOrDefault().Value;

            if (current == null)
            {
                return false;
            }

            return current.Frame + Constants.MAX_SFX_DELAY >= sfx.Frame;
        }

        public void Clear()
        {
            _gameContext.ClearDesiredSfx();
        }

        public void Reset()
        {
            _gameContext.ClearSfxs();
        }
    }
}
