using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.TowerFallExtensions.Entity;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Pickup))]
    public class PickupPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("FinishUnpack")]
        public static void Pickup_FinishUnpack(Pickup __instance)
        {
            var dynPickup = DynamicData.For(__instance);
            dynPickup.Set("FinishedUnpack", true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, typeof(Vector2), typeof(Vector2))]
        public static void Pickup_ctor(Pickup __instance, Vector2 targetPosition)
        {
            var dynPickup = DynamicData.For(__instance);
            dynPickup.Add("TargetPosition", targetPosition);
            dynPickup.Add("FinishedUnpack", false);

            ProperlySetTween(__instance, targetPosition);
        }

        private static void ProperlySetTween(Pickup self, Vector2 targetPosition)
        {
            self.DeleteComponent<Tween>();

            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 30, start: true);
            tween.OnStart = (tween.OnUpdate = delegate (Tween t)
            {
                self.TweenUpdate(t.Eased);
                self.Position = Vector2.Lerp(self.Position, targetPosition, t.Eased);
            });
            tween.OnComplete = self.FinishUnpack;
            self.Add(tween);
        }
    }
}
