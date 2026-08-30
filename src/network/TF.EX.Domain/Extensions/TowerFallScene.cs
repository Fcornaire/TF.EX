using HarmonyLib;
using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class TowerFallSceneExtensions
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
                return Traverse.Create(layer.Value).Field<List<Monocle.Entity>>("toAdd").Value;
            })
                .Where(ent => ent is T).Select(ent => ent as T);
        }

        public static T GetToBeSpawned<T>(this Monocle.Scene self) where T : Monocle.Entity
        {
            return self.Layers.SelectMany(layer =>
            {
                return Traverse.Create(layer.Value).Field<List<Monocle.Entity>>("toAdd").Value;
            })
                .FirstOrDefault(ent => ent is T) as T;
        }

        public static void RemoveToBeSpawned(this Monocle.Scene self, Monocle.Entity entity)
        {
            foreach (var layer in self.Layers)
            {
                var toAdd = Traverse.Create(layer.Value).Field<List<Monocle.Entity>>("toAdd").Value;

                if (toAdd.Remove(entity))
                {
                    return;
                }
            }
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

        public static void AddLoader(this Monocle.Scene scene, string msg, bool withFade = false)
        {
            var currentLoader = scene.Get<Loader>();
            if (currentLoader == null)
            {
                var loader = new Loader(true);
                loader.Depth = -1000;
                Loader.Message = msg;
                scene.Add(loader);
            }
            else
            {
                Loader.Message = msg;
            }

            if (withFade && scene.Get<CustomComponent.Fader>() == null)
            {
                scene.Add(new CustomComponent.Fader(-1, -500));
            }

            if (scene is TowerFall.MainMenu)
            {
                (scene as TowerFall.MainMenu).TweenUICameraToY(0);
            }
        }

        public static void RemoveLoader(this Monocle.Scene scene)
        {
            var loader = scene.Get<Loader>();
            Loader.Message = "";

            if (loader != null)
            {
                loader.RemoveSelf();
            }

            var fader = scene.Get<CustomComponent.Fader>();

            if (fader != null)
            {
                fader.RemoveSelf();
            }
        }

        public static Monocle.Layer GetGameplayLayer(this Level level)
        {
            return level.Layers.FirstOrDefault(l => l.Value.Index == Level.GAMEPLAY_LAYER).Value;
        }

        public static Monocle.Layer GetHUDLayer(this Level level)
        {
            return level.Layers.FirstOrDefault(l => l.Value.Index == Level.HUD_LAYER).Value;
        }
    }

    public static class TFGameExtensions
    {
        public static void ResetVersusChoices()
        {
            for (int i = 0; i < TFGame.Players.Length; i++)
            {
                TFGame.Players[i] = false;
            }

            for (int i = 0; i < TFGame.Characters.Length; i++)
            {
                TFGame.Characters[i] = i;
            }
        }
    }

    public static class XGamePadInputExtensions
    {
        public static Models.RightStick GetRightStick(this XGamepadInput input)
        {
            var xGamepad = input.XGamepad;
            var vector = xGamepad.GetRightStick();

            return new Models.RightStick
            {
                MoveX = (!(Math.Abs(vector.X) < 0.5f)) ? Math.Sign(vector.X) : 0,
                MoveY = (!(Math.Abs(vector.Y) < 0.8f)) ? Math.Sign(vector.Y) : 0,
                AimAxis = (vector.LengthSquared() < 0.09f) ? Microsoft.Xna.Framework.Vector2.Zero : vector
            };
        }
    }
}
