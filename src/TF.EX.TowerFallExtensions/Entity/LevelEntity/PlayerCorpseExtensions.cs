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
                ArrowCushion = entity.ArrowCushion.GetState(),
            };
        }

        public static void LoadState(this TowerFall.PlayerCorpse entity, PlayerCorpse toLoad)
        {
            var dynPlayerCorpse = DynamicData.For(entity);
            dynPlayerCorpse.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynPlayerCorpse.Set("Facing", (TowerFall.Facing)toLoad.Facing);
            dynPlayerCorpse.Set("actualDepth", toLoad.ActualDepth);

            entity.Position = toLoad.Position.ToTFVector();

            dynPlayerCorpse.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynPlayerCorpse.Set("KillerIndex", toLoad.KillerIndex);
            dynPlayerCorpse.Set("PlayerIndex", toLoad.PlayerIndex);
            entity.Speed = toLoad.Speed.ToTFVector();
            dynPlayerCorpse.Set("fallSpriteCounter", toLoad.FallSpriteCounter);
            entity.Pinned = toLoad.Pinned;

            entity.ArrowCushion.LoadState(toLoad.ArrowCushion);

            entity.ArrowCushion.RemoveArrows();
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
