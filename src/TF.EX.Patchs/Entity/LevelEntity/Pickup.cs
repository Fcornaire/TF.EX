using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.LevelEntity.Chest;
using TF.EX.Patchs.Extensions;
using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class PickupPatch : IHookable, IStateful<TowerFall.Pickup, Pickup>
    {


        public void Load()
        {
            On.TowerFall.Pickup.ctor += Pickup_ctor;
        }

        public void Unload()
        {
            On.TowerFall.Pickup.ctor -= Pickup_ctor;
        }

        private void Pickup_ctor(On.TowerFall.Pickup.orig_ctor orig, TowerFall.Pickup self, Vector2 position, Vector2 targetPosition)
        {
            orig(self, position, targetPosition);
            var dynPickup = DynamicData.For(self);
            dynPickup.Add("TargetPosition", targetPosition);

            ProperlySetTween(self, targetPosition);
        }

        private void ProperlySetTween(TowerFall.Pickup self, Vector2 targetPosition)
        {
            RemoveLastTween(self);

            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 30, start: true);
            tween.OnStart = (tween.OnUpdate = delegate (Tween t)
            {
                self.TweenUpdate(t.Eased);
                self.Position = Vector2.Lerp(self.Position, targetPosition, t.Eased);
            });
            tween.OnComplete = self.FinishUnpack;
            self.Add(tween);
        }

        private void RemoveLastTween(TowerFall.Pickup self)
        {
            foreach (var compo in self.Components.ToList())
            {
                if (compo is Tween)
                {
                    Tween tween = (Tween)compo;
                    tween.RemoveSelf();
                    self.Components.Remove(tween);
                }
            }
        }

        public Pickup GetState(TowerFall.Pickup entity)
        {
            var tween = entity.GetTween();
            var alarm = entity.GetAlarm();

            var dynPickup = DynamicData.For(entity);
            var actualDepth = dynPickup.Get<double>("actualDepth");
            var position = entity.Position.ToModel();
            var targetPosition = dynPickup.Get<Vector2>("TargetPosition").ToModel();
            var type = dynPickup.Get<TowerFall.Pickups>("PickupType");
            var sine = dynPickup.Get<SineWave>("sine");
            var markedForRemoval = dynPickup.Get<bool>("MarkedForRemoval");

            return new Pickup
            {
                ActualDepth = actualDepth,
                Position = position,
                TargetPosition = targetPosition,
                Type = type.ToModel(),
                //player_index = additionnalInfo.player_index,
                TargetPositionTimer = tween != null ? tween.FramesLeft : 0,
                CollidableTimer = alarm != null ? alarm.FramesLeft : 0,
                IsCollidable = entity.Collidable,
                SineCounter = sine.Counter,
                MarkedForRemoval = markedForRemoval
            };
        }

        public void LoadState(Pickup toLoad, TowerFall.Pickup entity)
        {
            var dynPickup = DynamicData.For(entity);
            dynPickup.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynPickup.Add("TargetPosition", toLoad.TargetPosition.ToTFVector());

            //dynPickup.Set("TargetPosition", toLoad.TargetPosition.ToTFVector());
            dynPickup.Set("PickupType", toLoad.Type.ToTFModel());
            dynPickup.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            entity.Collidable = toLoad.IsCollidable;

            var sine = dynPickup.Get<SineWave>("sine");
            sine.UpdateAttributes(toLoad.SineCounter);

            dynPickup.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            entity.Components.Remove(entity.GetTween());
            //RemoveLastTween(entity);

            entity.RemoveLastAlarm();

            if (toLoad.TargetPositionTimer > 0)
            {
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 30, start: true);
                var dynTween = DynamicData.For(tween);
                dynTween.Set("FramesLeft", toLoad.TargetPositionTimer);

                tween.OnUpdate = delegate (Tween t)
                {
                    entity.TweenUpdate(t.Eased);
                    entity.Position = Vector2.Lerp(entity.Position, toLoad.TargetPosition.ToTFVector(), t.Eased);

                };
                tween.OnComplete = entity.FinishUnpack;
                entity.Add(tween);
            }

            if (toLoad.CollidableTimer > 0)
            {
                Alarm.Set(entity, 10, () =>
                {
                    entity.Collidable = true;
                });

                var dynAlarm = DynamicData.For(entity.GetAlarm());
                dynAlarm.Set("FramesLeft", toLoad.CollidableTimer);
            }
        }
    }
}
