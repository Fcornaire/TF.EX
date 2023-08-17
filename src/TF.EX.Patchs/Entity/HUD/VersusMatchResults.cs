using MonoMod.Utils;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    public class VersusMatchResultsPatch : IHookable
    {
        private readonly IRngService _rngService;

        public VersusMatchResultsPatch(IRngService rngService)
        {
            _rngService = rngService;
        }

        public void Load()
        {
            On.TowerFall.VersusMatchResults.ctor += VersusMatchResults_ctor;
        }

        public void Unload()
        {
            On.TowerFall.VersusMatchResults.ctor -= VersusMatchResults_ctor;
        }

        private void VersusMatchResults_ctor(On.TowerFall.VersusMatchResults.orig_ctor orig, TowerFall.VersusMatchResults self, TowerFall.Session session, TowerFall.VersusRoundResults roundResults)
        {
            orig(self, session, roundResults);

            (TFGame.Instance.Scene as Level).Frozen = true;

            var dynVersusMatchResults = DynamicData.For(self);
            dynVersusMatchResults.Add("HasReset", false);

            session.MatchSettings.RandomLevelSeed = _rngService.GetSeed();

            var entity = new TowerFall.VersusSeedDisplay(session.MatchSettings.RandomSeedIcons);
            session.CurrentLevel.Layers[entity.LayerIndex].Entities.Add(entity);
        }
    }
}
