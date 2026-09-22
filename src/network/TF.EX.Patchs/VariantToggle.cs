using HarmonyLib;
using TF.EX.Domain.CustomComponent;
using TowerFall;

namespace TF.EX.Patchs
{
    // Refuse to switch on a modded variant that never declared rollback support
    [HarmonyPatch(typeof(VariantToggle))]
    public class VariantTogglePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnConfirm")]
        public static bool VariantToggle_OnConfirm(VariantToggle __instance)
        {
            return CanAllow(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("ShowPerPlayer")]
        public static bool VariantToggle_ShowPerPlayer(VariantToggle __instance)
        {
            return CanAllow(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VariantToggle.Render))]
        public static void VariantToggle_Render(VariantToggle __instance)
        {
            if (!MatchVariantsPatchs.IsRestricted(__instance?.Variant) && LackingPlayerName(__instance?.Variant) == null && !SeriesVariantLock.IsLocked())
            {
                return;
            }

            SeriesVariantLock.DrawLockedOverlay(__instance);
        }

        private static bool CanAllow(VariantToggle toggle)
        {
            if (MatchVariantsPatchs.IsRestricted(toggle?.Variant))
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, $"{toggle.Variant.Title} DOES NOT SUPPORT NETPLAY", 10, 400);

                return false;
            }

            if (SeriesVariantLock.IsLocked())
            {
                SeriesVariantLock.NotifyLocked();

                return false;
            }

            var lackingPlayer = LackingPlayerName(toggle?.Variant);

            if (lackingPlayer != null)
            {
                Sounds.ui_invalid.Play();
                Notification.Create(TFGame.Instance.Scene, $"{lackingPlayer} DOESN'T HAVE {toggle.Variant.Title}", 10, 400);

                return false;
            }

            return true;
        }

        private static string LackingPlayerName(TowerFall.Variant variant)
        {
            if (variant == null || variant.Value || !TF.EX.Domain.Context.LobbyBuilderContext.IsEditing)
            {
                return null;
            }

            var variants = TowerFall.MainMenu.VersusMatchSettings?.Variants;

            if (variants == null || !variants.CustomVariants.Any(pair => pair.Key.Contains('/') && ReferenceEquals(pair.Value, variant)))
            {
                return null;
            }

            var matchmakingService = TF.EX.Domain.ServiceCollections.ResolveMatchmakingService();
            var localPeerId = matchmakingService.GetRoomPeerId();

            return matchmakingService.GetOwnLobby().Players
                .FirstOrDefault(pl => pl.RoomPeerId != localPeerId && !pl.CustomVariants.Contains(variant.Title))
                ?.Name;
        }
    }
}
