using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Engine;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    public class VersusMatchResultsPatch : IHookable
    {
        private readonly IReplayService _replayService;
        private readonly IRngService _rngService;

        public VersusMatchResultsPatch(IReplayService replayService, IRngService rngService)
        {
            _replayService = replayService;
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

            if (!TFGamePatch.HasExported)
            {
                _replayService.Export();
                TFGamePatch.HasExported = true;
            }

            session.MatchSettings.RandomLevelSeed = _rngService.GetSeed();

            var entity = new TowerFall.VersusSeedDisplay(session.MatchSettings.RandomSeedIcons);
            session.CurrentLevel.Layers[entity.LayerIndex].Entities.Add(entity);
        }
    }
}
