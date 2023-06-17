using Monocle;
using System.Collections.Generic;
using System.Linq;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class EntityExtension
    {
        public static List<TreasureChest> GetTreasureChestToAdd(this Entity[] entities)
        {
            return entities.Where(entity => entity is TreasureChest).Select(ent => ent as TreasureChest).ToList();
        }

        public static Alarm GetAlarm(this Entity chest) //TODO: generics ?
        {
            foreach (var component in chest.Components)
            {
                if (component is Alarm)
                {
                    return (Alarm)component;
                }
            }
            return null;
        }

        public static Tween GetChestOpeningTween(this Entity chest)
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

        public static Tween GetTween(this Entity chest) //TODO: generics ?
        {
            foreach (var component in chest.Components)
            {
                if (component is Tween)
                {
                    return (Tween)component;
                }
            }
            return null;
        }

        public static void RemoveLastAlarm(this Entity entity)
        {
            foreach (var compo in entity.Components.ToArray())
            {
                if (compo is Alarm)
                {
                    Alarm alarm = (Alarm)compo;
                    alarm.RemoveSelf();
                    entity.Components.Remove(alarm);
                }
            }
        }

        public static StateMachine GetStateMachine(this Entity entity) //TODO: generics ?
        {
            foreach (var component in entity.Components)
            {
                if (component is StateMachine)
                {
                    return (StateMachine)component;
                }
            }
            return null;
        }

    }
}
