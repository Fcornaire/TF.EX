using TF.EX.TowerFallExtensions;

namespace TF.EX.Patchs
{
    internal class SaveDataPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.SaveData.Save += Save_Patch;
        }

        public void Unload()
        {
            On.TowerFall.SaveData.Save -= Save_Patch;
        }

        private string Save_Patch(On.TowerFall.SaveData.orig_Save orig, TowerFall.SaveData self)
        {
            self.WithNetplayOptions();
            return orig(self);
        }
    }
}
