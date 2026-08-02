using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Entity.MenuItem
{
    [HarmonyPatch(typeof(VersusModeButton))]
    public class VersusModeButtonPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void VersusModeButton_Update_Prefix(VersusModeButton __instance, out ModeSelection __state)
        {
            __state = ModeSelection.Capture(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void VersusModeButton_Update_Postfix(VersusModeButton __instance, ModeSelection __state)
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode;

            if (mode == __state.Mode || !mode.IsNetplay())
            {
                return;
            }

            __state.Restore(__instance);
        }

        public struct ModeSelection
        {
            public TowerFall.Modes Mode;
            public bool IsCustom;
            public string CustomVersusModeName;
            public int CurrentIndex;

            public static ModeSelection Capture(VersusModeButton button)
            {
                var settings = TowerFall.MainMenu.VersusMatchSettings;

                return new ModeSelection
                {
                    Mode = settings.Mode,
                    IsCustom = settings.IsCustom,
                    CustomVersusModeName = settings.CustomVersusModeName,
                    CurrentIndex = DynamicData.For(button).Get<int>("currentIndex"),
                };
            }

            public readonly void Restore(VersusModeButton button)
            {
                var settings = TowerFall.MainMenu.VersusMatchSettings;

                settings.Mode = Mode;
                settings.IsCustom = IsCustom;
                DynamicData.For(settings).Set("CustomVersusModeName", CustomVersusModeName);

                var dynButton = DynamicData.For(button);
                dynButton.Set("currentIndex", CurrentIndex);
                dynButton.Invoke("UpdateSides");
            }
        }
    }
}
