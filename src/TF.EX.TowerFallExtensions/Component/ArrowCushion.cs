using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Component;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.TowerFallExtensions.Component
{
    public static class ArrowCushionExtensions
    {
        public static ArrowCushion GetState(this TowerFall.ArrowCushion arrowCushion)
        {
            List<ArrowCushionData> arrowCushionData = new List<ArrowCushionData>();

            foreach (var arrow in arrowCushion.ArrowDatas.ToArray())
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

            var dynArrowCushion = DynamicData.For(arrowCushion);
            var lockDirection = dynArrowCushion.Get<bool>("lockDirection");
            var lockOffset = dynArrowCushion.Get<bool>("lockOffset");
            var offset = dynArrowCushion.Get<Vector2>("offset");
            var rotation = dynArrowCushion.Get<float>("rotation");

            var dynArrowCushionEntity = DynamicData.For(arrowCushion.Entity);
            var arrowCushionActualDepth = dynArrowCushionEntity.Get<double>("actualDepth");

            return new ArrowCushion
            {
                ArrowCushionDatas = arrowCushionData,
                LockDirection = lockDirection,
                LockOffset = lockOffset,
                Offset = offset.ToModel(),
                Rotation = rotation,
                EntityActualDepth = arrowCushionActualDepth,
            };
        }

        public static void LoadState(this TowerFall.ArrowCushion arrowCushion, ArrowCushion toLoad)
        {
            var dynArrowCushion = DynamicData.For(arrowCushion);
            dynArrowCushion.Set("offset", toLoad.Offset.ToTFVector());
            dynArrowCushion.Set("lockDirection", toLoad.LockDirection);
            dynArrowCushion.Set("lockOffset", toLoad.LockOffset);
            dynArrowCushion.Set("rotation", toLoad.Rotation);

            arrowCushion.RemoveArrows();
        }
    }
}
