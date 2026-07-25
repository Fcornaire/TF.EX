using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;
using TF.EX.TowerFallExtensions.Component;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
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
                Arrows = entity.Arrows.ToModel(),
                DropDir = dynPlayerCorpse.Get<int>("dropDir"),
                DropArrowAlarmFramesLeft = dropArrowAlarm != null ? dropArrowAlarm.FramesLeft : -1,
                Reviving = entity.Reviving,
                Revived = entity.Revived,
                Ledge = entity.Ledge,
                IgnoreJumpThrus = entity.IgnoreJumpThrus,
                Squished = entity.Squished.ToModel(),
                SquishedCounter = dynPlayerCorpse.Get<Monocle.Counter>("squishedCounter").Value,
                GhostSpawnCounter = dynPlayerCorpse.Get<float>("ghostSpawnCounter"),
            };
        }

        public static void LoadState(this TowerFall.PlayerCorpse entity, PlayerCorpse toLoad)
        {
            var dynPlayerCorpse = DynamicData.For(entity);
            dynPlayerCorpse.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            RemoveRevivers(entity);

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

            if (toLoad.Arrows != null)
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

                dynPlayerCorpse.Set("dropDir", toLoad.DropDir);
            }

            entity.ArrowCushion.LoadState(toLoad.ArrowCushion);

            entity.ArrowCushion.RemoveArrows();
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
