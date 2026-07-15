using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class ShiftBlockExtensions
    {
        public static ShiftBlock GetState(this TowerFall.ShiftBlock entity)
        {
            var dyn = DynamicData.For(entity);
            var counter = dyn.Get<Counter>("counter");

            return new ShiftBlock
            {
                Position = entity.Position.ToModel(),
                MoveFrom = dyn.Get<Vector2>("moveFrom").ToModel(),
                MoveTo = dyn.Get<Vector2>("moveTo").ToModel(),
                MoveLerp = dyn.Get<float>("moveLerp"),
                State = (int)dyn.Get<TowerFall.ShiftBlock.States>("state"),
                Counter = counter.GetState(),
                ActualDepth = dyn.Get<double>("actualDepth"),
            };
        }

        public static void LoadState(this TowerFall.ShiftBlock entity, ShiftBlock toLoad)
        {
            var dyn = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("moveFrom", toLoad.MoveFrom.ToTFVector());
            dyn.Set("moveTo", toLoad.MoveTo.ToTFVector());
            dyn.Set("moveLerp", toLoad.MoveLerp);
            dyn.Set("state", (TowerFall.ShiftBlock.States)toLoad.State);
            dyn.Set("actualDepth", toLoad.ActualDepth);

            var counter = dyn.Get<Counter>("counter");
            counter.LoadState(toLoad.Counter);
        }
    }
}
