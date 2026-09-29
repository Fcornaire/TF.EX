using HarmonyLib;
using TF.EX.Domain;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(Monocle.Music))]
    internal static class MusicPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.Play), [typeof(string)])]
        public static bool Music_Play(string filepath) => !InstantReplayFootage.HoldMusic(() => Monocle.Music.Play(filepath));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.Play), [typeof(string), typeof(bool)])]
        public static bool Music_Play_Looping(string filepath, bool looping) => !InstantReplayFootage.HoldMusic(() => Monocle.Music.Play(filepath, looping));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.PlayImmediate), [typeof(string)])]
        public static bool Music_PlayImmediate(string filepath) => !InstantReplayFootage.HoldMusic(() => Monocle.Music.PlayImmediate(filepath));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.PlayImmediate), [typeof(string), typeof(bool)])]
        public static bool Music_PlayImmediate_Looping(string filepath, bool looping) => !InstantReplayFootage.HoldMusic(() => Monocle.Music.PlayImmediate(filepath, looping));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.PlayNext), [typeof(string), typeof(bool)])]
        public static bool Music_PlayNext(string filepath, bool looping) => !InstantReplayFootage.HoldMusic(() => Monocle.Music.PlayNext(filepath, looping));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Monocle.Music.Stop))]
        public static bool Music_Stop() => !InstantReplayFootage.HoldMusic(Monocle.Music.Stop);
    }
}
