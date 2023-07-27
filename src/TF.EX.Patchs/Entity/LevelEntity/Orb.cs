using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    internal class OrbPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Orb.ctor_Vector2_bool += Orb_ctor_Vector2_bool;
        }

        public void Unload()
        {
            On.TowerFall.Orb.ctor_Vector2_bool -= Orb_ctor_Vector2_bool;
        }

        private void Orb_ctor_Vector2_bool(On.TowerFall.Orb.orig_ctor_Vector2_bool orig, TowerFall.Orb self, Vector2 position, bool explodes)
        {
            orig(self, position, explodes);

            var dynOrb = DynamicData.For(self);
            dynOrb.Set("ownerIndex", -1);
        }
    }
}
