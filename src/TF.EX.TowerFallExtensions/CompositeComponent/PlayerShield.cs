using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.TowerFallExtensions.CompositeComponent
{
    public static class PlayerShieldExtensions
    {
        public static PlayerShield GetState(this TowerFall.PlayerShield entity)
        {
            var dynEntity = DynamicData.For(entity);
            var shield = dynEntity.Get<Monocle.SpritePart<int>>("sprite");
            var sineWave = dynEntity.Get<Monocle.SineWave>("sine");

            return new PlayerShield
            {
                Shield = shield != null ? shield.GetState() : null,
                SineCounter = sineWave.Counter,
            };
        }

        public static void LoadState(this TowerFall.PlayerShield entity, PlayerShield toLoad)
        {
            var dynEntity = DynamicData.For(entity);

            var shieldSprite = dynEntity.Get<Monocle.SpritePart<int>>("sprite");
            if (shieldSprite != null && toLoad.Shield != null)
            {
                shieldSprite.LoadState(toLoad.Shield);
            }

            var sineWave = dynEntity.Get<Monocle.SineWave>("sine");
            sineWave.UpdateAttributes(toLoad.SineCounter);
        }
    }
}
