using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Chest;
using TowerFall;
using static TowerFall.TreasureChest;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class TreasureChestExtensions
    {
        public static Chest GetState(this TreasureChest entity)
        {
            var alarm = entity.GetComponent<Alarm>();
            var tween = entity.GetChestOpeningTween();

            var dynTreasureChest = DynamicData.For(entity);
            var actualDepth = dynTreasureChest.Get<double>("actualDepth");
            var sprite = dynTreasureChest.Get<Sprite<int>>("sprite");
            var appearCounter = dynTreasureChest.Get<Counter>("appearCounter");
            var pickups = dynTreasureChest.Get<List<Pickups>>("pickups");
            var vSpeed = dynTreasureChest.Get<float>("vSpeed");
            var positionCounter = dynTreasureChest.Get<Vector2>("counter");

            return new Chest
            {
                ActualDepth = actualDepth,
                CurrentAnimId = sprite.CurrentAnimID,
                Position = entity.Position.ToModel(),
                AppearCounter = appearCounter.Value,
                Pickups = pickups[0].ToModel(), //TODO: Giant Chest ?
                State = entity.State.ToModel(),
                VSpeed = vSpeed,
                PositionCounter = positionCounter.ToModel(),
                AppearTimer = alarm != null ? alarm.FramesLeft : 0,
                IsCollidable = entity.Collidable,
                IsLightVisible = entity.LightVisible,
                OpeningTimer = tween != null ? tween.FramesLeft : 0
            };
        }

        public static void LoadState(this TreasureChest entity, Chest toLoad)
        {
            var dynTreasureChest = DynamicData.For(entity);
            dynTreasureChest.Set("Scene", TFGame.Instance.Scene);

            entity.Added();

            dynTreasureChest.Set("actualDepth", toLoad.ActualDepth);
            var counter = dynTreasureChest.Get<Counter>("appearCounter");
            var appearCounter = DynamicData.For(counter);
            appearCounter.Set("counter", toLoad.AppearCounter);
            entity.Position = toLoad.Position.ToTFVector();

            var pickups = dynTreasureChest.Get<List<Pickups>>("pickups");
            pickups[0] = toLoad.Pickups.ToTFModel();
            dynTreasureChest.Set("pickups", pickups);

            dynTreasureChest.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynTreasureChest.Set("vSpeed", toLoad.VSpeed);
            dynTreasureChest.Set("State", toLoad.State.ToTFModel());

            entity.Collidable = toLoad.IsCollidable;
            entity.LightVisible = toLoad.IsLightVisible;
            entity.Visible = toLoad.IsLightVisible;

            var sprite = dynTreasureChest.Get<Sprite<int>>("sprite");
            sprite.Play(toLoad.CurrentAnimId);

            entity.DeleteComponent<Alarm>();
            entity.DeleteComponent<Tween>();

            if (entity.State == States.Appearing)
            {
                entity.LightVisible = true;
                entity.Visible = true;
                Alarm.Set(entity, (int)toLoad.AppearTimer, delegate
                {
                    var dynTreasureChestAlarm = DynamicData.For(entity);

                    dynTreasureChestAlarm.Set("State", States.Closed);
                    entity.Seek = entity.Level.Session.MatchSettings.SoloMode;
                    entity.Collidable = true;

                    var type = dynTreasureChestAlarm.Get<Types>("type");

                    if (type == Types.Large)
                    {
                        Sounds.sfx_chestAppearBig.Stop();
                    }
                });
            }

            if (entity.State <= States.Closed)
            {
                if (!entity.Tags.Contains(GameTags.PlayerCollider))
                {
                    entity.Tag(GameTags.PlayerCollider, GameTags.PlayerGhostCollider);
                }
            }

            if (entity.State == States.Opening)
            {
                entity.LightVisible = false;
                entity.Visible = true;
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 40, start: true); //TODO: Tween.Set
                var dynTween = DynamicData.For(tween);
                dynTween.Set("FramesLeft", toLoad.OpeningTimer);

                var light = dynTreasureChest.Get<Sprite<int>>("light");

                tween.OnUpdate = delegate (Tween t)
                {
                    light.Color = Color.Lerp(Color.White, Color.Transparent, t.Eased);
                    float amount = Ease.BackIn(t.Percent);
                    sprite.Scale.X = MathHelper.Lerp(0.8f, 1f, amount);
                    sprite.Scale.Y = MathHelper.Lerp(1.2f, 1f, amount);
                };
                tween.OnComplete = delegate
                {
                    light.Visible = (light.Active = false);
                    dynTreasureChest.Set("state", States.Opened);
                };
                entity.Add(tween);
            }

            if (entity.State == States.Opened)
            {
                entity.Visible = true;
            }
        }
    }
}
