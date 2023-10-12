using MonoMod.Utils;
using TowerFall;

namespace TF.EX.TowerFallExtensions.Scene
{
    public static class SceneExtensions
    {
        public static T Get<T>(this Monocle.Scene self) where T : Monocle.Entity
        {
            return self.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;
        }

        public static IEnumerable<T> GetAll<T>(this Monocle.Scene scene) where T : Monocle.Entity
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T);
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

        public static void DeleteAll<T>(this Monocle.Scene scene) where T : Monocle.Entity
        {
            var entities = scene.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is T).Select(ent => ent as T).ToList();

            if (entities.Count > 0)
            {
                entities.ForEach(entity =>
                {
                    scene.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                    entity.Removed();
                });
            }
        }

        public static void AddLoader(this Monocle.Scene scene, string msg)
        {
            var loader = new Loader(true);
            Loader.Message = msg;
            scene.Add(loader);
        }

        public static void RemoveLoader(this Monocle.Scene scene)
        {
            var loader = scene.Get<Loader>();
            Loader.Message = "";

            if (loader != null)
            {
                loader.RemoveSelf();
            }
        }
    }
}
