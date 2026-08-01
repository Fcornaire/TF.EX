using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class DummyExtensions
    {
        public static Dummy GetState(this TowerFall.Dummy entity)
        {
            var dyn = DynamicData.For(entity);

            return new Dummy
            {
                Position = entity.Position.ToModel(),
                ActualDepth = dyn.Get<double>("actualDepth"),
                Facing = (int)entity.Facing,
                Dead = dyn.Get<bool>("dead"),
                IsCollidable = entity.Collidable,
                IsVisible = entity.Visible,
            };
        }

        public static void LoadState(this TowerFall.Dummy entity, Dummy toLoad)
        {
            var dyn = DynamicData.For(entity);

            entity.Position = toLoad.Position.ToTFVector();
            entity.Facing = (TowerFall.Facing)toLoad.Facing;
            entity.Collidable = toLoad.IsCollidable;
            entity.Visible = toLoad.IsVisible;

            dyn.Set("dead", toLoad.Dead);

            EnsureHead(entity, dyn, toLoad.Facing);

            dyn.Set("actualDepth", toLoad.ActualDepth);
        }

        private static void EnsureHead(TowerFall.Dummy entity, DynamicData dyn, int facing)
        {
            if (dyn.Get<Monocle.Image>("head") != null)
            {
                return;
            }

            var head = TowerFall.TFGame.SpriteData.GetImage("DummyHead");
            head.Scale.X = facing;

            entity.Add(head);
            dyn.Set("head", head);
        }
    }
}
