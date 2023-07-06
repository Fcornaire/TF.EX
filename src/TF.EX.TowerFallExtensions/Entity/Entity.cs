using Monocle;
using TowerFall;

namespace TF.EX.TowerFallExtensions.Entity
{
    public static class EntityExtensions
    {
        public static T GetComponent<T>(this Monocle.Entity entity) where T : Component
        {
            return entity.Components.FirstOrDefault(component => component is T) as T;
        }

        public static void DeleteComponent<T>(this Monocle.Entity entity) where T : Component
        {
            var component = entity.GetComponent<T>();
            if (component != null)
            {
                component.RemoveSelf();
                entity.Components.Remove(component);
            }
        }

        public static void DeleteAllComponents<T>(this Monocle.Entity entity) where T : Component
        {
            var components = entity.Components.Where(component => component is T).ToList();
            foreach (var component in components)
            {
                component.RemoveSelf();
                entity.Components.Remove(component);
            }
        }

        public static Tween GetChestOpeningTween(this Monocle.Entity chest)
        {
            foreach (var component in chest.Components)
            {
                if (component is Tween && (chest as TreasureChest).State == TreasureChest.States.Opening)
                {
                    return (Tween)component;
                }
            }
            return null;
        }
    }
}
