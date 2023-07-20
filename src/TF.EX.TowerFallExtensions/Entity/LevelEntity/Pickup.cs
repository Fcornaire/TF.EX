using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class PickupExtensions
    {
        public static Pickup GetState(this TowerFall.Pickup entity)
        {
            var tween = entity.GetComponent<Tween>();
            var alarm = entity.GetComponent<Alarm>();

            var dynPickup = DynamicData.For(entity);
            var actualDepth = dynPickup.Get<double>("actualDepth");
            var position = entity.Position.ToModel();
            var targetPosition = dynPickup.Get<Vector2>("TargetPosition").ToModel();
            var type = dynPickup.Get<TowerFall.Pickups>("PickupType");
            var sine = dynPickup.Get<Monocle.SineWave>("sine");
            var markedForRemoval = dynPickup.Get<bool>("MarkedForRemoval");

            TF.EX.Domain.Models.State.Sprite<int> shieldSprite = null;

            if (type == TowerFall.Pickups.Shield)
            {
                var sprite = dynPickup.Get<Monocle.Sprite<int>>("sprite");
                shieldSprite = sprite.GetState();
            }

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
                MarkedForRemoval = markedForRemoval,
                ShieldSprite = shieldSprite
            };
        }

        public static void LoadState(this TowerFall.Pickup entity, Pickup toLoad)
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

            var sine = dynPickup.Get<Monocle.SineWave>("sine");
            sine.UpdateAttributes(toLoad.SineCounter);

            dynPickup.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            entity.DeleteComponent<Tween>();
            entity.DeleteAllComponents<Alarm>(); //TODO: Prevent removing ChangeColorAlarm

            if (toLoad.Type == PickupState.Shield)
            {
                var sprite = dynPickup.Get<Monocle.Sprite<int>>("sprite");
                sprite.LoadState(toLoad.ShieldSprite);
            }

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
                Alarm.Set(entity, (int)toLoad.CollidableTimer, () =>
                {
                    entity.Collidable = true;
                });
            }
        }
    }
}
