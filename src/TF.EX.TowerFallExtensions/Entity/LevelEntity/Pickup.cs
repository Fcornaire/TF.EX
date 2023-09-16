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
            var sine = dynPickup.Get<SineWave>("sine");
            var markedForRemoval = dynPickup.Get<bool>("MarkedForRemoval");

            var finishUnpack = dynPickup.Get<bool>("FinishedUnpack");

            TF.EX.Domain.Models.State.Sprite<int> sprite = null;

            if (type == TowerFall.Pickups.Shield)
            {
                sprite = dynPickup.Get<Sprite<int>>("sprite").GetState();
            }
            else if (type == TowerFall.Pickups.Bomb)
            {
                sprite = dynPickup.Get<Sprite<int>>("image").GetState();
            }

            return new Pickup
            {
                ActualDepth = actualDepth,
                Position = position,
                TargetPosition = targetPosition,
                Type = type.ToModel(),
                //player_index = additionnalInfo.player_index,
                TweenTimer = tween != null ? tween.FramesLeft : 0,
                CollidableTimer = alarm != null ? alarm.FramesLeft : 0,
                IsCollidable = entity.Collidable,
                SineCounter = sine.Counter,
                MarkedForRemoval = markedForRemoval,
                Sprite = sprite,
                FinishedUnpack = finishUnpack,
            };
        }

        public static void LoadState(this TowerFall.Pickup entity, Pickup toLoad)
        {
            var dynPickup = DynamicData.For(entity);
            dynPickup.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynPickup.Add("TargetPosition", toLoad.TargetPosition.ToTFVector());
            dynPickup.Add("FinishedUnpack", toLoad.FinishedUnpack);

            //dynPickup.Set("TargetPosition", toLoad.TargetPosition.ToTFVector());
            dynPickup.Set("PickupType", toLoad.Type.ToTFModel());
            dynPickup.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            entity.Collidable = toLoad.IsCollidable;

            var sine = dynPickup.Get<SineWave>("sine");
            sine.UpdateAttributes(toLoad.SineCounter);

            dynPickup.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            entity.DeleteComponent<Tween>();
            entity.DeleteAllComponents<Alarm>(); //TODO: Prevent removing ChangeColorAlarm

            if (toLoad.Type == PickupState.Shield)
            {
                var sprite = dynPickup.Get<Sprite<int>>("sprite");
                sprite.LoadState(toLoad.Sprite);
            }
            else if (toLoad.Type == PickupState.Bomb)
            {
                var sprite = dynPickup.Get<Sprite<int>>("image");
                sprite.LoadState(toLoad.Sprite);
            }

            if (toLoad.TweenTimer > 0)
            {
                if (!toLoad.FinishedUnpack)
                {
                    //TargetPosition tween
                    Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BackOut, 30, start: true);
                    var dynTween = DynamicData.For(tween);
                    dynTween.Set("FramesLeft", toLoad.TweenTimer);

                    tween.OnUpdate = delegate (Tween t)
                    {
                        entity.TweenUpdate(t.Eased);
                        entity.Position = Vector2.Lerp(entity.Position, toLoad.TargetPosition.ToTFVector(), t.Eased);

                    };
                    tween.OnComplete = entity.FinishUnpack;
                    entity.Add(tween);
                }
                else
                {
                    var bombPickup = entity as TowerFall.BombPickup;
                    var dynBombPickup = DynamicData.For(bombPickup);

                    Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 40, start: true);
                    var dynTween = DynamicData.For(tween);
                    dynTween.Set("FramesLeft", toLoad.TweenTimer);

                    var image = dynBombPickup.Get<Sprite<int>>("image");

                    tween.OnUpdate = delegate (Tween t)
                    {
                        image.Scale = Vector2.One * MathHelper.Lerp(1f, 3f, t.Eased);
                        image.Rate = MathHelper.Lerp(1f, 4f, t.Eased);
                        image.Rotation = MathHelper.Lerp(0f, (float)Math.PI * 2f, t.Eased);
                    };
                    tween.OnComplete = (tween) => dynBombPickup.Invoke("Explode", tween);
                    entity.Add(tween);
                }
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
