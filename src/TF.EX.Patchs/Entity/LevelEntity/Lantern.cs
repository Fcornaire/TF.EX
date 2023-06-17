using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LanternPatch : IStateful<TowerFall.Lantern, Lantern>
    {

        public Lantern GetState(TowerFall.Lantern entity)
        {
            var dynLantern = DynamicData.For(entity);
            var actualDepth = dynLantern.Get<double>("actualDepth");
            var dead = dynLantern.Get<bool>("dead");
            var falling = dynLantern.Get<bool>("falling");
            var vSpeed = dynLantern.Get<float>("vSpeed");
            var positionCounter = dynLantern.Get<Vector2>("counter");
            var position = dynLantern.Get<Vector2>("Position");
            var collidable = dynLantern.Get<bool>("Collidable");

            return new Lantern
            {
                ActualDepth = actualDepth,
                IsCollidable = collidable,
                IsDead = dead,
                IsFalling = falling,
                Position = position.ToModel(),
                PositionCounter = positionCounter.ToModel(),
                VSpeed = vSpeed
            };
        }

        public void LoadState(Lantern toLoad, TowerFall.Lantern entity)
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
            if (!falling)
            {
                dynLantern.Invoke("CheckForChain");
                ReTag(entity);
            }

            dynLantern.Set("actualDepth", actualDepth);
            dynLantern.Set("dead", dead);
            dynLantern.Set("falling", falling);
            dynLantern.Set("vSpeed", vSpeed);
            dynLantern.Set("counter", positionCounter);
            dynLantern.Set("Position", position);
            dynLantern.Set("Collidable", collidable);
        }

        /// <summary>
        /// Falling detag, tag are used to check for collision, so we re tag if not falling
        /// </summary>
        private void ReTag(TowerFall.Lantern entity)
        {
            if (!entity.Tags.Contains(Monocle.GameTags.Target))
            {
                entity.Tag(Monocle.GameTags.Target);
            }

            if (!entity.Tags.Contains(Monocle.GameTags.ExplosionCollider))
            {
                entity.Tag(Monocle.GameTags.ExplosionCollider);
            }

            if (!entity.Tags.Contains(Monocle.GameTags.PlayerCollider))
            {
                entity.Tag(Monocle.GameTags.PlayerCollider);
            }
        }
    }
}
