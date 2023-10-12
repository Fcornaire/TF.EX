using Monocle;
using TowerFall;

namespace TF.EX.Patchs
{
    internal class VariantPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.Variant.ctor += Variant_ctor;
        }

        public void Unload()
        {
            On.TowerFall.Variant.ctor -= Variant_ctor;
        }

        private void Variant_ctor(On.TowerFall.Variant.orig_ctor orig, TowerFall.Variant self, Subtexture icon, string title, string description, Pickups[] itemExclusions, bool perPlayer, string header, TowerFall.UnlockData.Unlocks? unlocker, bool scrollEffect, bool hidden, bool canRandom, bool tournamentRule1v1, bool tournamentRule2v2, bool unlisted, bool darkWorldDLC, int coOpValue)
        {
            orig(self, icon, title, description, itemExclusions, false, header, unlocker, scrollEffect, hidden, canRandom, tournamentRule1v1, tournamentRule2v2, unlisted, darkWorldDLC, coOpValue);
        }
    }
}
