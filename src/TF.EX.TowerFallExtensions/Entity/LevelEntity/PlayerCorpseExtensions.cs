using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

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

            var dynArrowCushion = DynamicData.For(entity.ArrowCushion);
            var lockDirection = dynArrowCushion.Get<bool>("lockDirection");
            var lockOffset = dynArrowCushion.Get<bool>("lockOffset");
            var offset = dynArrowCushion.Get<Vector2>("offset");
            var rotation = dynArrowCushion.Get<float>("rotation");

            return new PlayerCorpse
            {
                ActualDepth = actualDepth,
                Facing = facing,
                Position = entity.Position.ToModel(),
                PositionCounter = counter.ToModel(),
                KillerIndex = entity.KillerIndex,
                PlayerIndex = entity.PlayerIndex,
                Speed = entity.Speed.ToModel(),
                FallSpriteCounter = fallSpriteCounter,
                Pinned = entity.Pinned,
                ArrowCushion = new ArrowCushion
                {
                    ArrowCushionDatas = arrowCushionData,
                    LockDirection = lockDirection,
                    LockOffset = lockOffset,
                    Offset = offset.ToModel(),
                    Rotation = rotation
                },
            };
        }

        public static void LoadState(this TowerFall.PlayerCorpse entity, PlayerCorpse toLoad)
        {
            var dynPlayerCorpse = DynamicData.For(entity);
            dynPlayerCorpse.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynPlayerCorpse.Set("Facing", toLoad.Facing);
            dynPlayerCorpse.Set("actualDepth", toLoad.ActualDepth);

            entity.Position = toLoad.Position.ToTFVector();

            dynPlayerCorpse.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynPlayerCorpse.Set("KillerIndex", toLoad.KillerIndex);
            dynPlayerCorpse.Set("PlayerIndex", toLoad.PlayerIndex);
            entity.Speed = toLoad.Speed.ToTFVector();
            dynPlayerCorpse.Set("fallSpriteCounter", toLoad.FallSpriteCounter);
            entity.Pinned = toLoad.Pinned;

            var dynArrowCushion = DynamicData.For(entity.ArrowCushion);
            dynArrowCushion.Set("offset", toLoad.ArrowCushion.Offset.ToTFVector());
            dynArrowCushion.Set("lockDirection", toLoad.ArrowCushion.LockDirection);
            dynArrowCushion.Set("lockOffset", toLoad.ArrowCushion.LockOffset);
            dynArrowCushion.Set("rotation", toLoad.ArrowCushion.Rotation);

            entity.ArrowCushion.RemoveArrows();
        }

        public static Monocle.Entity GetEntityByDepth(double actualDepth)
        {
            Monocle.Entity entity = null;
            foreach (Monocle.Entity ent in (TowerFall.TFGame.Instance.Scene as TowerFall.Level).GetGameplayLayer().Entities.ToArray())
            {
                var dynEntity = DynamicData.For(ent);
                var entActualDepth = dynEntity.Get<double>("actualDepth");
                if (entActualDepth == actualDepth)
                {
                    entity = ent;
                    break;
                }
            }

            return entity;
        }

        public static void LoadArrowCushion(this TowerFall.PlayerCorpse corpse, PlayerCorpse toLoad)
        {
            foreach (var arrowData in toLoad.ArrowCushion.ArrowCushionDatas.ToArray())
            {
                var gameArrow = GetEntityByDepth(arrowData.ActualDepth) as TowerFall.Arrow;

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
