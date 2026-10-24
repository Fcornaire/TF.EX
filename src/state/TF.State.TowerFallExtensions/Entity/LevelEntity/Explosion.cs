using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;
using TF.State.TowerFallExtensions.ComponentExtensions;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
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
                BombTrap = explosion.BombTrap,
                SharedCounters = GetSharedCounters(counters),
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
            var sprites = dynExplosion.Get<List<Sprite<int>>>("sprites");
            var sharedCounters = toLoad.SharedCounters ?? Enumerable.Repeat(-1, toLoadCounters.Count).ToArray();

            if (!HasLayout(counters, sprites, sharedCounters))
            {
                RebuildLayout(dynExplosion, counters, sprites, sharedCounters);
            }

            for (int i = 0; i < counters.Count; i++)
            {
                counters[i].LoadState(toLoadCounters[i]);
            }

            alarm.LoadState(toLoad.Alarm);

            dynExplosion.Set("super", toLoad.IsSuper);
            explosion.PlayerIndex = toLoad.PlayerIndex;
            explosion.Kills = toLoad.Kills;

            dynExplosion.Set("TriggerBomb", toLoad.TriggerBomb);
            dynExplosion.Set("BombTrap", toLoad.BombTrap);
        }

        private static int[] GetSharedCounters(List<Counter> counters)
        {
            var shared = counters.Select((counter, index) => counters.FindIndex(0, index, other => ReferenceEquals(other, counter))).ToArray();

            return shared.Any(index => index >= 0) ? shared : null;
        }

        private static bool HasLayout(List<Counter> counters, List<Sprite<int>> sprites, int[] sharedCounters)
        {
            if (counters.Count != sharedCounters.Length || sprites.Count != sharedCounters.Length || sprites.Any(sprite => sprite == null))
            {
                return false;
            }

            var current = GetSharedCounters(counters) ?? [.. Enumerable.Repeat(-1, counters.Count)];

            return current.SequenceEqual(sharedCounters);
        }

        private static void RebuildLayout(DynamicData dynExplosion, List<Counter> counters, List<Sprite<int>> sprites, int[] sharedCounters)
        {
            var rebuiltCounters = new List<Counter>();
            var rebuiltSprites = new List<Sprite<int>>();

            for (int i = 0; i < sharedCounters.Length; i++)
            {
                rebuiltCounters.Add(sharedCounters[i] >= 0 ? rebuiltCounters[sharedCounters[i]] : new Counter());
                rebuiltSprites.Add(i < sprites.Count && sprites[i] != null
                    ? sprites[i]
                    : (Sprite<int>)dynExplosion.Invoke("GenSprite", "ExplosionKill" + TowerFall.Lava.Suffix));
            }

            counters.Clear();
            counters.AddRange(rebuiltCounters);
            sprites.Clear();
            sprites.AddRange(rebuiltSprites);
        }
    }
}
