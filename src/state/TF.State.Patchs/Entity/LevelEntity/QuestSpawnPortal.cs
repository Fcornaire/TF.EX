using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions.Entity;
using TowerFall;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(QuestSpawnPortal))]
    public class QuestSpawnPortalPatch
    {
        public const float NotTweening = -1f;
        private const float TWEEN_FRAMES = 40f;

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Vector2), typeof(Vector2[])])]
        public static void QuestSpawnPortal_ctor(QuestSpawnPortal __instance)
        {
            DynamicData.For(__instance).Add("portalAppearCounter", NotTweening);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Appear")]
        public static void QuestSpawnPortal_Appear_Postfix(QuestSpawnPortal __instance, bool __result)
        {
            StartOwnedTween(__instance, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Disappear")]
        public static void QuestSpawnPortal_Disappear_Postfix(QuestSpawnPortal __instance, bool __result)
        {
            StartOwnedTween(__instance, __result);
        }

        private static void StartOwnedTween(QuestSpawnPortal portal, bool didChange)
        {
            if (!didChange || !StateFlags.IsCaptureActive)
            {
                return;
            }

            DynamicData.For(portal).Set("portalAppearCounter", 0f);
            portal.DeleteAllComponents<Tween>();
            ApplyTweenPose(portal, 0f);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void QuestSpawnPortal_Update_Postfix(QuestSpawnPortal __instance)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            __instance.DeleteAllComponents<Tween>();

            var dyn = DynamicData.For(__instance);
            var counter = dyn.Get<float>("portalAppearCounter");
            if (counter == NotTweening)
            {
                return;
            }

            counter += Monocle.Engine.TimeMult;
            ApplyTweenPose(__instance, counter);

            if (counter >= TWEEN_FRAMES)
            {
                counter = NotTweening;
            }

            dyn.Set("portalAppearCounter", counter);
        }

        public static void ApplyTweenPose(QuestSpawnPortal portal, float counter)
        {
            var dyn = DynamicData.For(portal);
            var appeared = dyn.Get<bool>("appeared");
            var percent = Math.Min(Math.Max(counter, 0f), TWEEN_FRAMES) / TWEEN_FRAMES;
            var scale = appeared ? Ease.BackOut(percent) : 1f - Ease.BackIn(percent);

            dyn.Get<Monocle.Sprite<int>>("sprite").Scale = Vector2.One * scale;
            portal.LightAlpha = scale;
        }
    }
}
