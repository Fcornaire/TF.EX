using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class LanternExtensions
    {
        public static Lantern GetState(this TowerFall.Lantern entity)
        {
            var dynLantern = DynamicData.For(entity);
            var actualDepth = dynLantern.Get<double>("actualDepth");
            var dead = dynLantern.Get<bool>("dead");
            var falling = dynLantern.Get<bool>("falling");
            var vSpeed = dynLantern.Get<float>("vSpeed");
            var positionCounter = dynLantern.Get<Vector2>("counter");
            var position = dynLantern.Get<Vector2>("Position");
            var collidable = dynLantern.Get<bool>("Collidable");
            var chain = dynLantern.Get<TowerFall.Chain>("Chain");

            return new Lantern
            {
                ActualDepth = actualDepth,
                IsCollidable = collidable,
                IsDead = dead,
                IsFalling = falling,
                Position = position.ToModel(),
                PositionCounter = positionCounter.ToModel(),
                VSpeed = vSpeed,
                ChainActualDepth = chain != null ? DynamicData.For(chain).Get<double>("actualDepth") : 0,
            };
        }

        public static void LoadState(this TowerFall.Lantern entity, Lantern toLoad)
        {
            var dynLantern = DynamicData.For(entity);

            double actualDepth;
            bool dead;
            bool falling;
            float vSpeed;
            Vector2 positionCounter;
            Vector2 position;
            bool collidable;

            actualDepth = toLoad.ActualDepth;
            collidable = toLoad.IsCollidable;
            dead = toLoad.IsDead;
            falling = toLoad.IsFalling;
            position = toLoad.Position.ToTFVector();
            vSpeed = toLoad.VSpeed;
            positionCounter = toLoad.PositionCounter.ToTFVector();

            dynLantern.Set("actualDepth", actualDepth);
            dynLantern.Set("dead", dead);
            dynLantern.Set("falling", falling);
            dynLantern.Set("vSpeed", vSpeed);
            dynLantern.Set("counter", positionCounter);
            dynLantern.Set("Position", position);
            dynLantern.Set("Collidable", collidable);

            if (!falling)
            {
                if (toLoad.ChainActualDepth != 0)
                {
                    var chain = entity.Level.GetEntityByDepth(toLoad.ChainActualDepth) as TowerFall.Chain;
                    if (chain != null)
                    {
                        dynLantern.Set("Chain", chain);
                        chain.Holding = entity;
                    }
                }
                else
                {
                    dynLantern.Invoke("CheckForChain");
                }

                entity.ReTag();
            }
            else
            {
                var heldChain = dynLantern.Get<TowerFall.Chain>("Chain");
                if (heldChain != null)
                {
                    heldChain.Holding = null;
                    dynLantern.Set("Chain", null);
                }

                entity.UntagInstant(Monocle.GameTags.Target);
                entity.UntagInstant(Monocle.GameTags.ExplosionCollider);
                entity.UntagInstant(Monocle.GameTags.PlayerCollider);
            }
        }

        private static void ReTag(this TowerFall.Lantern entity)
        {
            entity.TagInstant(Monocle.GameTags.Target);
            entity.TagInstant(Monocle.GameTags.ExplosionCollider);
            entity.TagInstant(Monocle.GameTags.PlayerCollider);
        }

        private static void TagInstant(this TowerFall.Lantern entity, Monocle.GameTags tag)
        {
            if (!entity.Tags.Contains(tag))
            {
                entity.Tags.Add(tag);

                var tagList = entity.Level[tag];
                if (!tagList.Contains(entity))
                {
                    tagList.Add(entity);
                }
            }
        }

        private static void UntagInstant(this TowerFall.Lantern entity, Monocle.GameTags tag)
        {
            entity.Tags.Remove(tag);
            entity.Level[tag].Remove(entity);
        }
    }
}
