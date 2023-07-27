using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class OrbExtensions
    {
        public static Orb GetState(this TowerFall.Orb entity)
        {
            var dynOrb = DynamicData.For(entity);
            var actualDepth = dynOrb.Get<double>("actualDepth");
            var falling = dynOrb.Get<bool>("falling");
            var vSpeed = dynOrb.Get<float>("vSpeed");
            var positionCounter = dynOrb.Get<Vector2>("counter");
            var position = dynOrb.Get<Vector2>("Position");
            var collidable = dynOrb.Get<bool>("Collidable");
            var sine = dynOrb.Get<SineWave>("sine");
            var ownerIndex = dynOrb.Get<int>("ownerIndex");

            return new Orb
            {
                ActualDepth = actualDepth,
                IsCollidable = collidable,
                IsFalling = falling,
                Position = position.ToModel(),
                PositionCounter = positionCounter.ToModel(),
                VSpeed = vSpeed,
                SineCounter = sine.Counter,
                OwnerIndex = ownerIndex
            };
        }

        public static void LoadState(this TowerFall.Orb entity, Orb toLoad)
        {
            var dynOrb = DynamicData.For(entity);

            dynOrb.Set("actualDepth", toLoad.ActualDepth);
            dynOrb.Set("falling", toLoad.IsFalling);
            dynOrb.Set("vSpeed", toLoad.VSpeed);
            dynOrb.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynOrb.Set("Position", toLoad.Position.ToTFVector());
            dynOrb.Set("Collidable", toLoad.IsCollidable);
            dynOrb.Set("ownerIndex", toLoad.OwnerIndex);

            var playerOrCorpse = entity.Level.GetPlayerOrCorpse(toLoad.OwnerIndex);
            entity.CannotHit = playerOrCorpse;

            var sine = dynOrb.Get<SineWave>("sine");
            sine.UpdateAttributes(toLoad.SineCounter);
        }
    }
}
