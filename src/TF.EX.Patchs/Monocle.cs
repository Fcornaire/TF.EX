using On.Monocle;
using System.Reflection;

namespace TF.EX.Patchs
{
    internal class MonoclePatch : IHookable
    {
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

            var field = typeof(Monocle.Audio).GetField("pitchList", BindingFlags.NonPublic | BindingFlags.Static);
            var pitchList = (List<Monocle.SFX>)field.GetValue(null);

            pitchList.ToList().ForEach(pitch =>
            {
                pitch.Stop();
            });
        }
    }
}
