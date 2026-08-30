using HarmonyLib;
using TF.Replay.Domain;
using TF.Replay.Domain.CustomComponent;
using TowerFall;

namespace TF.Replay.Patchs.Scene
{
    //Custom prompt for end of replay  instead of match results
    [HarmonyPatch(typeof(Level))]
    internal static class LevelHostEndPrompt
    {
        private static EndOfReplayPrompt _prompt;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Level_Update(Level __instance)
        {
            if (StandalonePlayback.IsActive)
            {
                _prompt = null;
                return;
            }

            var service = ServiceCollections.ResolveReplayService();

            if (service == null || !service.IsPlayback)
            {
                _prompt = null;
                return;
            }

            if ((Modes)(service.GetReplay()?.Informations?.Mode ?? -1) == Modes.Trials)
            {
                return;
            }

            var over = service.LastFrame > 0 && service.PlaybackFrame > service.LastFrame;

            if (over && _prompt == null && !Takeover.SuppressesPlaybackChecks)
            {
                __instance.Add(_prompt = new EndOfReplayPrompt());

                return;
            }

            if (!over && _prompt != null)
            {
                if (_prompt.Scene != null)
                {
                    _prompt.RemoveSelf();
                }

                _prompt = null;
            }
        }
    }
}
