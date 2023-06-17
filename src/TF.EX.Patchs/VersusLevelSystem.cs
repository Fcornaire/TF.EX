using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs
{
    public class VersusLevelSystemPatch : IHookable
    {
        private readonly IRngService _rngService;

        public VersusLevelSystemPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

        public void Load()
        {
            On.TowerFall.VersusLevelSystem.ctor += VersusLevelSystem_ctor;
            On.TowerFall.VersusLevelSystem.GenLevels += VersusLevelSystem_GenLevels;
        }

        public void Unload()
        {
            On.TowerFall.VersusLevelSystem.ctor -= VersusLevelSystem_ctor;
            On.TowerFall.VersusLevelSystem.GenLevels -= VersusLevelSystem_GenLevels;
        }

        /// <summary>Same as original but using custom one since the original is not using random from calc</summary>
        private void VersusLevelSystem_GenLevels(On.TowerFall.VersusLevelSystem.orig_GenLevels orig, TowerFall.VersusLevelSystem self, TowerFall.MatchSettings matchSettings)
        {
            var dynVersusLevelSystem = DynamicData.For(self);
            var lastLevel = dynVersusLevelSystem.Get<string>("lastLevel");
            var levels = self.OwnGenLevel(matchSettings, self.VersusTowerData, lastLevel, _rngService);

            dynVersusLevelSystem.Set("levels", levels);
        }

        private void VersusLevelSystem_ctor(On.TowerFall.VersusLevelSystem.orig_ctor orig, TowerFall.VersusLevelSystem self, TowerFall.VersusTowerData tower)
        {
            orig(self, tower);

            var dynVersusLevelSystem = DynamicData.For(self);
            dynVersusLevelSystem.Set("ShowControls", false);
            dynVersusLevelSystem.Set("ShowTriggerControls", false);
        }



    }
}
