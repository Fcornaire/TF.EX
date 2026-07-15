using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class SwitchBlockExtensions
    {
        public static SwitchBlock GetState(this TowerFall.SwitchBlock entity)
        {
            var dyn = DynamicData.For(entity);

            return new SwitchBlock
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                On = dyn.Get<bool>("on"),
                IsCollidable = entity.Collidable,
            };
        }

        public static void LoadState(this TowerFall.SwitchBlock entity, SwitchBlock toLoad)
        {
            var dyn = DynamicData.For(entity);

            dyn.Set("on", toLoad.On);
            entity.Collidable = toLoad.IsCollidable;

            var onMap = dyn.Get<Tilemap>("onMap");
            var offMap = dyn.Get<Tilemap>("offMap");
            if (onMap != null)
            {
                onMap.Visible = toLoad.On;
                if (toLoad.On)
                {
                    onMap.Color = Color.White * (toLoad.IsCollidable ? 1f : 0.5f);
                }
            }
            if (offMap != null)
            {
                offMap.Visible = !toLoad.On;
            }
        }
    }
}
