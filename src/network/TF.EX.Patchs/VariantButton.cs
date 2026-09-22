using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Patchs
{
    internal static class SeriesVariantLock
    {
        private static readonly Type[] LockedButtons =
        [
            typeof(VariantDisableAll),
            typeof(VariantRandomize),
            typeof(VariantTournament1v1),
            typeof(VariantTournament2v2),
            typeof(VariantPreset),
            typeof(FortRise.ModVariantPreset),
            typeof(FortRise.VariantPresetCustom),
            typeof(FortRise.VariantPresetAdd),
        ];

        public static IEnumerable<MethodBase> GetMethod(string methodName)
        {
            return LockedButtons
                .Select(type => AccessTools.DeclaredMethod(type, methodName))
                .Where(method => method != null);
        }

        public static bool IsLocked()
        {
            return TFGame.Instance.Scene is MainMenu menu
                && menu.State.ToDomainModel() == TF.EX.Domain.Models.MenuState.LobbyBuilder
                && TF.EX.Domain.ServiceCollections.ResolveMatchmakingService().GetOwnLobby().IsSeriesLobby;
        }

        public static void NotifyLocked()
        {
            Sounds.ui_invalid.Play();
            Notification.Create(TFGame.Instance.Scene, "LOCKED (SERIES ON)", 10, 400);
        }

        public static void DrawLockedOverlay(VariantItem item)
        {
            Draw.Rect(item.X - 10f, item.Y - 10f, 20f, 20f, Color.Black * 0.6f * item.Alpha);
        }
    }

    [HarmonyPatch]
    public class VariantButtonConfirmPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return SeriesVariantLock.GetMethod("OnConfirm");
        }

        [HarmonyPrefix]
        public static bool VariantButton_OnConfirm()
        {
            if (!SeriesVariantLock.IsLocked())
            {
                return true;
            }

            SeriesVariantLock.NotifyLocked();

            return false;
        }
    }

    [HarmonyPatch]
    public class VariantButtonRenderPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return SeriesVariantLock.GetMethod(nameof(VariantButton.Render));
        }

        [HarmonyPostfix]
        public static void VariantButton_Render(VariantButton __instance)
        {
            if (!SeriesVariantLock.IsLocked())
            {
                return;
            }

            SeriesVariantLock.DrawLockedOverlay(__instance);
        }
    }
}
