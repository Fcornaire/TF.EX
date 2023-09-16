using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.TowerFallExtensions.Entity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class PickupPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Pickup.ctor += Pickup_ctor;
            On.TowerFall.Pickup.FinishUnpack += Pickup_FinishUnpack;
        }

        public void Unload()
        {
            On.TowerFall.Pickup.ctor -= Pickup_ctor;
            On.TowerFall.Pickup.FinishUnpack -= Pickup_FinishUnpack;
        }

        private void Pickup_FinishUnpack(On.TowerFall.Pickup.orig_FinishUnpack orig, TowerFall.Pickup self, Tween t)
        {
            orig(self, t);
            var dynPickup = DynamicData.For(self);
            dynPickup.Set("FinishedUnpack", true);
        }

        private void Pickup_ctor(On.TowerFall.Pickup.orig_ctor orig, TowerFall.Pickup self, Vector2 position, Vector2 targetPosition)
        {
            orig(self, position, targetPosition);
            var dynPickup = DynamicData.For(self);
            dynPickup.Add("TargetPosition", targetPosition);
            dynPickup.Add("FinishedUnpack", false);

            ProperlySetTween(self, targetPosition);
        }

        private void ProperlySetTween(TowerFall.Pickup self, Vector2 targetPosition)
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
