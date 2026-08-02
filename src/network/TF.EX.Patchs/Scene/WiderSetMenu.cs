using FortRise;
using HarmonyLib;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public static class WiderSetMenu
    {
        private const string ModuleTypeName = "Teuria.WiderSet.WiderSetModule";
        private const string SelectionEntryProperty = "StandardSelectionEntry";

        private static readonly string[] SelectionButtonTypeNames =
        {
            "Teuria.WiderSet.StandardSetButton",
            "Teuria.WiderSet.WideSetButton",
        };

        private static bool resolved;
        private static MainMenu.MenuState? selectionState;

        public static MainMenu.MenuState? SelectionState
        {
            get
            {
                if (!resolved)
                {
                    resolved = true;
                    selectionState = Resolve();
                }

                return selectionState;
            }
        }

        public static bool IsSelectionState(MainMenu.MenuState state)
        {
            return SelectionState.HasValue && SelectionState.Value == state;
        }

        public static void PatchSelectionButtons(IHarmony harmony)
        {
            var postfix = new HarmonyMethod(typeof(WiderSetMenu), nameof(SelectionButton_MenuAction));

            foreach (var typeName in SelectionButtonTypeNames)
            {
                var type = AccessTools.TypeByName(typeName);
                var menuAction = type == null ? null : AccessTools.DeclaredMethod(type, "MenuAction");

                if (menuAction != null)
                {
                    harmony.Patch(menuAction, null, postfix, null, null);
                }
            }
        }

        public static void SelectionButton_MenuAction(TowerFall.MenuItem __instance)
        {
            if (!OnlinePlayToggle.IsOn)
            {
                return;
            }

            __instance.MainMenu.State = TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel();
        }

        private static MainMenu.MenuState? Resolve()
        {
            try
            {
                var moduleType = AccessTools.TypeByName(ModuleTypeName);

                if (moduleType == null)
                {
                    return null;
                }

                var entry = AccessTools.Property(moduleType, SelectionEntryProperty)?.GetValue(null) as IMenuStateEntry;

                return entry?.MenuState;
            }
            catch
            {
                return null;
            }
        }
    }
}
