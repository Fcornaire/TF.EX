using MonoMod.Utils;

namespace TF.EX.TowerFallExtensions.Scene
{
    public static class SceneExtensions
    {
        public static T Get<T>(this Monocle.Scene self) where T : Monocle.Entity
        {
            return self.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;
        }

        public static IEnumerable<T> GetAllToBeSpawned<T>(this Monocle.Scene self) where T : Monocle.Entity
        {
            return self.Layers.SelectMany(layer =>
            {
                var dynLayer = DynamicData.For(layer.Value);

                return dynLayer.Get<List<Monocle.Entity>>("toAdd");
            })
                .Where(ent => ent is T).Select(ent => ent as T);
        }

        public static T GetToBeSpawned<T>(this Monocle.Scene self) where T : Monocle.Entity
        {
            return self.Layers.SelectMany(layer =>
            {
                var dynLayer = DynamicData.For(layer.Value);

                return dynLayer.Get<List<Monocle.Entity>>("toAdd");

            })
                .FirstOrDefault(ent => ent is T) as T;
        }

        public static void DeleteAll<T>(this Monocle.Scene level) where T : Monocle.Entity
        {
            var entities = level.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T).ToList();

            if (entities.Count > 0)
            {
                entities.ForEach(entity =>
                {
                    level.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                    entity.Removed();
                });
            }
        }
    }
}
