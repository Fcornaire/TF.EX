using Monocle;
using MonoMod.Utils;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    internal class ShieldPickupPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.ShieldPickup.ChangeColor += ShieldPickup_ChangeColor;
        }

        public void Unload()
        {
            On.TowerFall.ShieldPickup.ChangeColor -= ShieldPickup_ChangeColor;
        }

        //TODO: fix this (causing shield to change color)
        private void ShieldPickup_ChangeColor(On.TowerFall.ShieldPickup.orig_ChangeColor orig, TowerFall.ShieldPickup self)
        {
            var dynShieldPickup = DynamicData.For(self);
            var sprite = dynShieldPickup.Get<Sprite<int>>("sprite");

            if (sprite != null) //Work around manual added from Pickup class (since this isn't yet created
            {
                orig(self);
            }
        }
    }
}
