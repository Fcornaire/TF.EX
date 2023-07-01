using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Engine;

namespace TF.EX.Patchs.Entity.HUD
{
    public class VersusMatchResultsPatch : IHookable
    {
        private readonly IReplayService _replayService;
        private readonly IRngService _rngService;
        private readonly INetplayManager _netplayManager;

        public VersusMatchResultsPatch(IReplayService replayService, IRngService rngService, INetplayManager netplayManager)
        {
            _replayService = replayService;
            _rngService = rngService;
            _netplayManager = netplayManager;
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
