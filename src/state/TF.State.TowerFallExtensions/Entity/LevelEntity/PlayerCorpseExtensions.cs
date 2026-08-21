using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity.Player;
using TF.State.TowerFallExtensions.Component;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class PlayerCorpseExtensions
    {
        public static PlayerCorpse GetState(this TowerFall.PlayerCorpse entity)
        {

            List<ArrowCushionData> arrowCushionData = new List<ArrowCushionData>();

            foreach (var arrow in entity.ArrowCushion.ArrowDatas.ToArray())
            {
                var dynArrow = DynamicData.For(arrow.Arrow);

                var data = new ArrowCushionData
                {
                    ActualDepth = dynArrow.Get<double>("actualDepth"),
                    Offset = arrow.Offset.ToModel(),
                    Rotation = arrow.Rotation,
                };
                arrowCushionData.Add(data);
            }

            var dynPlayerCorpse = DynamicData.For(entity);
            var actualDepth = dynPlayerCorpse.Get<double>("actualDepth");
            var facing = dynPlayerCorpse.Get<TowerFall.Facing>("Facing");
            var counter = dynPlayerCorpse.Get<Vector2>("counter");
            var fallSpriteCounter = dynPlayerCorpse.Get<float>("fallSpriteCounter");
            var dropArrowAlarm = dynPlayerCorpse.Get<Monocle.Alarm>("dropArrowAlarm");

            return new PlayerCorpse
            {
                ActualDepth = actualDepth,
                Facing = (Facing)facing,
                Position = entity.Position.ToModel(),
                PositionCounter = counter.ToModel(),
                KillerIndex = entity.KillerIndex,
                PlayerIndex = entity.PlayerIndex,
                Speed = entity.Speed.ToModel(),
                FallSpriteCounter = fallSpriteCounter,
                Pinned = entity.Pinned,
                AgainstWall = dynPlayerCorpse.Get<bool>("againstWall"),
                ArrowCushion = entity.ArrowCushion.GetState(),
                ExplodingCounter = dynPlayerCorpse.Get<float>("explodingCounter"),
                Arrows = entity.Arrows?.ToModel(),
                DropDir = dynPlayerCorpse.Get<int>("dropDir"),
                DropArrowAlarmFramesLeft = dropArrowAlarm != null ? dropArrowAlarm.FramesLeft : -1,
                Reviving = entity.Reviving,
                Revived = entity.Revived,
                Ledge = entity.Ledge,
                IgnoreJumpThrus = entity.IgnoreJumpThrus,
                Squished = entity.Squished.ToModel(),
                SquishedCounter = dynPlayerCorpse.Get<Monocle.Counter>("squishedCounter").Value,
                GhostSpawnCounter = dynPlayerCorpse.Get("ghostSpawnCounter") as float? ?? -1f,
                PrismHit = entity.PrismHit,
                PrismFall = dynPlayerCorpse.Get<bool>("prismFall"),
                PrismTicks = dynPlayerCorpse.Get("prismTicks") as float? ?? -1f,
                HasBrambles = dynPlayerCorpse.Get<Monocle.FlashingImage[]>("brambles") != null,
                BrambleTicks = dynPlayerCorpse.Get("brambleTicks") as float? ?? -1f,
                BrambleCollidable = entity.BrambleCollidable,
                BramblesVisible = dynPlayerCorpse.Get<bool>("bramblesVisible"),
                NotCollidable = !entity.Collidable,
                NotPushable = !entity.Pushable,
                DodgeTooLateCounter = dynPlayerCorpse.Get<Monocle.Counter>("dodgeTooLateCounter").Value,
                Depth = entity.Depth,
            };
        }

        public static void LoadState(this TowerFall.PlayerCorpse entity, PlayerCorpse toLoad)
        {
            var dynPlayerCorpse = DynamicData.For(entity);
            dynPlayerCorpse.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            RemoveRevivers(entity);

            var keepAlarm = dynPlayerCorpse.Get<Monocle.Alarm>("dropArrowAlarm");
            foreach (var component in entity.Components
                .Where(c => (c is Monocle.Alarm && c != keepAlarm) || c is Monocle.Tween || c is Monocle.Coroutine)
                .ToList())
            {
                entity.Remove(component);
            }
            dynPlayerCorpse.Set("prismCoroutine", null);

            dynPlayerCorpse.Set("Facing", (TowerFall.Facing)toLoad.Facing);
            dynPlayerCorpse.Set("actualDepth", toLoad.ActualDepth);

            entity.Position = toLoad.Position.ToTFVector();

            dynPlayerCorpse.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynPlayerCorpse.Set("KillerIndex", toLoad.KillerIndex);
            dynPlayerCorpse.Set("PlayerIndex", toLoad.PlayerIndex);
            entity.Speed = toLoad.Speed.ToTFVector();
            dynPlayerCorpse.Set("fallSpriteCounter", toLoad.FallSpriteCounter);
            entity.Pinned = toLoad.Pinned;
            dynPlayerCorpse.Set("againstWall", toLoad.AgainstWall);
            dynPlayerCorpse.Set("explodingCounter", toLoad.ExplodingCounter);

            entity.Reviving = toLoad.Reviving;
            entity.Revived = toLoad.Revived;
            entity.Ledge = toLoad.Ledge;
            entity.IgnoreJumpThrus = toLoad.IgnoreJumpThrus;
            dynPlayerCorpse.Set("Squished", toLoad.Squished.ToTFVector());
            DynamicData.For(dynPlayerCorpse.Get<Monocle.Counter>("squishedCounter")).Set("counter", toLoad.SquishedCounter);

            dynPlayerCorpse.Set("ghostSpawnCounter", toLoad.GhostSpawnCounter);

            if (toLoad.Arrows != null && entity.Arrows != null)
            {
                entity.Arrows.ToLoad(toLoad.Arrows);

                var dropArrowAlarm = dynPlayerCorpse.Get<Monocle.Alarm>("dropArrowAlarm");
                if (toLoad.DropArrowAlarmFramesLeft >= 0)
                {
                    if (dropArrowAlarm == null)
                    {
                        entity.StartDroppingArrows();
                        dropArrowAlarm = dynPlayerCorpse.Get<Monocle.Alarm>("dropArrowAlarm");
                    }
                    if (dropArrowAlarm != null)
                    {
                        DynamicData.For(dropArrowAlarm).Set("FramesLeft", toLoad.DropArrowAlarmFramesLeft);
                    }
                }
                else if (dropArrowAlarm != null)
                {
                    entity.Remove(dropArrowAlarm);
                    dynPlayerCorpse.Set("dropArrowAlarm", null);
                }
            }

            dynPlayerCorpse.Set("dropDir", toLoad.DropDir);

            entity.Collidable = !toLoad.NotCollidable;
            entity.Pushable = !toLoad.NotPushable;
            DynamicData.For(dynPlayerCorpse.Get<Monocle.Counter>("dodgeTooLateCounter")).Set("counter", toLoad.DodgeTooLateCounter);

            if (toLoad.Depth != 0)
            {
                dynPlayerCorpse.Set("depth", toLoad.Depth);
            }

            dynPlayerCorpse.Set("PrismHit", toLoad.PrismHit);
            dynPlayerCorpse.Set("prismFall", toLoad.PrismHit && toLoad.PrismFall);
            dynPlayerCorpse.Set("prismTicks", toLoad.PrismHit ? toLoad.PrismTicks : -1f);

            LoadBrambleState(entity, dynPlayerCorpse, toLoad);

            entity.ArrowCushion.LoadState(toLoad.ArrowCushion);

            entity.ArrowCushion.RemoveArrows();
        }

        private static void LoadBrambleState(TowerFall.PlayerCorpse entity, DynamicData dynPlayerCorpse, PlayerCorpse toLoad)
        {
            if (!toLoad.HasBrambles)
            {
                dynPlayerCorpse.Set("brambleTicks", -1f);
                dynPlayerCorpse.Set("BrambleCollidable", false);
                dynPlayerCorpse.Set("bramblesVisible", false);
                dynPlayerCorpse.Set("brambles", null);

                if (entity.Tags.Contains(Monocle.GameTags.PlayerCollider))
                {
                    entity.Tags.Remove(Monocle.GameTags.PlayerCollider);
                    entity.Level[Monocle.GameTags.PlayerCollider].Remove(entity);
                }

                return;
            }

            if (dynPlayerCorpse.Get<Monocle.FlashingImage[]>("brambles") == null)
            {
                var brambles = new Monocle.FlashingImage[2];
                for (int i = 0; i < brambles.Length; i++)
                {
                    var image = new Monocle.FlashingImage(TowerFall.TFGame.Atlas["brambles"]);
                    image.CenterOrigin();
                    image.Scale = Vector2.One * Monocle.Calc.Range(Monocle.Calc.Random, 0.8f, 0.4f);
                    image.Position = new Vector2(Monocle.Calc.Range(Monocle.Calc.Random, -2f, 4f), 4f + Monocle.Calc.Range(Monocle.Calc.Random, -2f, 4f));
                    image.Rotation = Monocle.Calc.NextAngle(Monocle.Calc.Random);
                    entity.Add(image);
                    image.Visible = false;
                    brambles[i] = image;
                }
                dynPlayerCorpse.Set("brambles", brambles);
            }

            dynPlayerCorpse.Set("brambleTicks", toLoad.BrambleTicks);
            dynPlayerCorpse.Set("BrambleCollidable", toLoad.BrambleCollidable);
            dynPlayerCorpse.Set("bramblesVisible", toLoad.BramblesVisible);

            if (!entity.Tags.Contains(Monocle.GameTags.PlayerCollider))
            {
                entity.Tags.Add(Monocle.GameTags.PlayerCollider);
                var tagList = entity.Level[Monocle.GameTags.PlayerCollider];
                if (!tagList.Contains(entity))
                {
                    tagList.Add(entity);
                }
            }
        }

        private static void RemoveRevivers(TowerFall.PlayerCorpse corpse)
        {
            if (corpse.Level == null)
            {
                return;
            }

            foreach (var layer in corpse.Level.Layers.Values)
            {
                var toAdd = DynamicData.For(layer).Get<List<Monocle.Entity>>("toAdd");
                toAdd?.RemoveAll(entity => entity is TowerFall.TeamReviver reviver && reviver.Corpse == corpse);
            }

            corpse.Level[Monocle.GameTags.TeamReviver].RemoveAll(entity => entity is TowerFall.TeamReviver reviver && reviver.Corpse == corpse);
        }

        public static void LoadArrowCushionDatas(this TowerFall.PlayerCorpse corpse, PlayerCorpse toLoad)
        {
            foreach (var arrowData in toLoad.ArrowCushion.ArrowCushionDatas.ToArray())
            {
                var gameArrow = (TowerFall.TFGame.Instance.Scene as TowerFall.Level).GetEntityByDepth(arrowData.ActualDepth) as TowerFall.Arrow;

                if (gameArrow != null)
                {
                    var inGameArrowData = new TowerFall.ArrowCushion.ArrowData
                    {
                        Arrow = gameArrow,
                        Offset = arrowData.Offset.ToTFVector(),
                        Rotation = arrowData.Rotation
                    };
                    corpse.ArrowCushion.ArrowDatas.Add(inGameArrowData);
                }
            }
        }
    }
}
