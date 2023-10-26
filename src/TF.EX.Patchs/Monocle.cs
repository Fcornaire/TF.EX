using Microsoft.Extensions.Logging;
using On.Monocle;
using System.Reflection;
using TF.EX.Common.Extensions;

namespace TF.EX.Patchs
{
    internal class MonoclePatch : IHookable
    {
        private ILogger _logger;

        public MonoclePatch(ILogger logger)
        {
            _logger = logger;
        }

        public void Load()
        {
            On.Monocle.Audio.Stop += Audio_Stop;
        }

        public void Unload()
        {
            On.Monocle.Audio.Stop -= Audio_Stop;
        }

        private void Audio_Stop(Audio.orig_Stop orig)
        {
            try
            {
                var field = typeof(Monocle.Audio).GetField("pitchList", BindingFlags.NonPublic | BindingFlags.Static);
                var pitchList = (List<Monocle.SFX>)field.GetValue(null);

                pitchList.ToList().ForEach(pitch =>
                {
                    pitch.Stop();
                });
            }
            catch (Exception e)
            {
                _logger.LogError<MonoclePatch>("Error stopping audio", e);
            }
        }
    }
}
