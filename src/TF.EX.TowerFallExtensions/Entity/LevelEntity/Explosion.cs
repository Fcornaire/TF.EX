using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.TowerFallExtensions.ComponentExtensions;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class ExplosionExtensions
    {
        public static Explosion GetState(this TowerFall.Explosion explosion)
        {
            var dynExplosion = DynamicData.For(explosion);
            var counters = dynExplosion.Get<List<Counter>>("counters");
            var alarm = dynExplosion.Get<Alarm>("alarm");
            var super = dynExplosion.Get<bool>("super");
            var actualDepth = dynExplosion.Get<double>("actualDepth");
            var position = dynExplosion.Get<Vector2>("Position").ToModel();


            return new Explosion
            {
                ActualDepth = actualDepth,
                Position = position,
                Counters = counters.Select(counter => counter.GetState()).ToList(),
                Alarm = alarm.GetState(),
                IsSuper = super,
                PlayerIndex = explosion.PlayerIndex,
                Kills = explosion.Kills,
                TriggerBomb = explosion.TriggerBomb,
                BombTrap = explosion.BombTrap
            };
        }

        public static void LoadState(this TowerFall.Explosion explosion, Explosion toLoad)
        {
            var dynExplosion = DynamicData.For(explosion);
            var counters = dynExplosion.Get<List<Counter>>("counters");
            var alarm = dynExplosion.Get<Alarm>("alarm");

            explosion.Position = toLoad.Position.ToTFVector();
            dynExplosion.Set("actualDepth", toLoad.ActualDepth);

            var toLoadCounters = toLoad.Counters.ToList();
            for (int i = 0; i < counters.Count; i++)
            {
                if (i < toLoadCounters.Count)
                {
                    counters[i].LoadState(toLoadCounters[i]);
                }
            }

            alarm.LoadState(toLoad.Alarm);

            dynExplosion.Set("super", toLoad.IsSuper);
            explosion.PlayerIndex = toLoad.PlayerIndex;
            explosion.Kills = toLoad.Kills;

            dynExplosion.Set("TriggerBomb", toLoad.TriggerBomb);
            dynExplosion.Set("BombTrap", toLoad.BombTrap);
        }
    }
}
