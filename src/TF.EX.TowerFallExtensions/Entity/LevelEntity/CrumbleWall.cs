using System.Reflection;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Platform;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class CrumbleWallExtensions
    {
        private static readonly MethodInfo FinishBreakMethod = typeof(TowerFall.CrumbleWall)
            .GetMethod("FinishBreak", BindingFlags.NonPublic | BindingFlags.Instance);

        public static CrumbleWall GetState(this TowerFall.CrumbleWall entity)
        {
            var dyn = DynamicData.For(entity);
            var alarm = entity.GetComponent<Monocle.Alarm>();

            return new CrumbleWall
            {
                ActualDepth = dyn.Get<double>("actualDepth"),
                Position = entity.Position.ToModel(),
                Destroyed = dyn.Get<bool>("destroyed"),
                Shaking = dyn.Get<bool>("shaking"),
                IsCollidable = entity.Collidable,
                BreakAlarm = alarm != null ? alarm.GetState() : null,
            };
        }

        public static void LoadState(this TowerFall.CrumbleWall entity, CrumbleWall toLoad)
        {
            var dyn = DynamicData.For(entity);

            if (entity.Level == null)
            {
                dyn.Set("Scene", TowerFall.TFGame.Instance.Scene);
                dyn.Set("Level", TowerFall.TFGame.Instance.Scene as TowerFall.Level);
            }

            entity.Position = toLoad.Position.ToTFVector();
            dyn.Set("actualDepth", toLoad.ActualDepth);
            dyn.Set("destroyed", toLoad.Destroyed);
            dyn.Set("shaking", toLoad.Shaking);
            entity.Collidable = toLoad.IsCollidable;

            var alarm = entity.GetComponent<Monocle.Alarm>();

            if (toLoad.BreakAlarm == null)
            {
                if (alarm != null)
                {
                    alarm.RemoveSelf();
                }

                return;
            }

            if (alarm == null)
            {
                var onComplete = (Action)Delegate.CreateDelegate(typeof(Action), entity, FinishBreakMethod);
                alarm = Monocle.Alarm.Set(entity, toLoad.BreakAlarm.Duration, onComplete);
            }

            alarm.LoadState(toLoad.BreakAlarm);
        }
    }
}
