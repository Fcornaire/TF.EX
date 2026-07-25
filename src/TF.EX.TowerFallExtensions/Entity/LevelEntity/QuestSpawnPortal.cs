using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.Entity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class QuestSpawnPortalExtensions
    {
        public static QuestSpawnPortal GetState(this TowerFall.QuestSpawnPortal entity)
        {
            var dynPortal = DynamicData.For(entity);

            return new QuestSpawnPortal
            {
                ActualDepth = dynPortal.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Appeared = dynPortal.Get<bool>("appeared"),
                ToSpawn = dynPortal.Get<Queue<string>>("toSpawn").ToList(),
                LastFacing = (int)dynPortal.Get<TowerFall.Facing>("lastFacing"),
                AutoDisappear = dynPortal.Get<bool>("autoDisappear"),
                AddCounter = dynPortal.Get<Counter>("addCounter").Value,
                Sprite = dynPortal.Get<Sprite<int>>("sprite").GetState(),
                AppearCounter = dynPortal.Get<float>("portalAppearCounter"),
            };
        }

        public static void LoadState(this TowerFall.QuestSpawnPortal entity, QuestSpawnPortal toLoad)
        {
            var dynPortal = DynamicData.For(entity);
            dynPortal.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynPortal.Set("actualDepth", toLoad.ActualDepth);
            entity.Position = toLoad.Position.ToTFVector();
            dynPortal.Set("appeared", toLoad.Appeared);
            dynPortal.Set("toSpawn", new Queue<string>(toLoad.ToSpawn));
            dynPortal.Set("lastFacing", (TowerFall.Facing)toLoad.LastFacing);
            dynPortal.Set("autoDisappear", toLoad.AutoDisappear);
            DynamicData.For(dynPortal.Get<Counter>("addCounter")).Set("counter", toLoad.AddCounter);

            dynPortal.Set("portalAppearCounter", toLoad.AppearCounter);

            var sprite = dynPortal.Get<Sprite<int>>("sprite");
            sprite.LoadState(toLoad.Sprite);

            entity.DeleteAllComponents<Tween>();
            entity.LightAlpha = sprite.Scale.X;
        }
    }
}
