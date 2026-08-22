using HarmonyLib;
using TF.InputDisplayer.Domain;
using TowerFall;

namespace TF.InputDisplayer.Patchs
{
    [HarmonyPatch(typeof(ReplayFrame))]
    internal static class ReplayFramePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Record")]
        public static void ReplayFrame_Record(ReplayFrame __instance)
        {
            if (!DisplayOptions.Enabled || !DisplayOptions.ShowInInstantReplay)
            {
                return;
            }

            if (SaveData.Instance != null && SaveData.Instance.Options.ShowInputDuringReplays)
            {
                return;
            }

            __instance.Input ??= new InputState[InputHistory.MaxSeats];

            for (int seat = 0; seat < __instance.Input.Length; seat++)
            {
                var attached = seat < TFGame.Players.Length && TFGame.Players[seat] && seat < TFGame.PlayerInputs.Length && TFGame.PlayerInputs[seat] != null;

                __instance.Input[seat] = attached ? TFGame.PlayerInputs[seat].GetState() : default;
            }
        }
    }
}
