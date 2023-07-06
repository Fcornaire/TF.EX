using Monocle;
using TF.EX.TowerFallExtensions.Entity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class ShieldPickupPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.ShieldPickup.Added += ShieldPickup_Added;
        }

        public void Unload()
        {
            On.TowerFall.ShieldPickup.Added -= ShieldPickup_Added;
        }

        private void ShieldPickup_Added(On.TowerFall.ShieldPickup.orig_Added orig, TowerFall.ShieldPickup self)
        {
            orig(self);

            self.DeleteComponent<Alarm>(); //TODO: Prevent removing ChangeColorAlarm
        }
    }
}
