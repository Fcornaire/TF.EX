using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class LevelLoaderXMLPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IHUDService _hudService;

        public LevelLoaderXMLPatch(INetplayManager netplayManager, IHUDService hUDService)
        {
            _netplayManager = netplayManager;
            _hudService = hUDService;
        }

        public void Load()
        {
            On.TowerFall.LevelLoaderXML.ctor += LevelLoaderXML_ctor;
        }

        public void Unload()
        {
            On.TowerFall.LevelLoaderXML.ctor -= LevelLoaderXML_ctor;
        }

        private void LevelLoaderXML_ctor(On.TowerFall.LevelLoaderXML.orig_ctor orig, LevelLoaderXML self, TowerFall.Session session)
        {
            orig(self, session);
            Reset(session);
        }

        /// <summary>
        /// Reset for next round
        /// </summary>
        /// <param name="session"></param>
        private void Reset(TowerFall.Session session)
        {
            ServiceCollections.ResetState();

            if (TFGame.Instance.Scene != null && TFGame.Instance.Scene is TowerFall.Level)
            {
                (TFGame.Instance.Scene as TowerFall.Level).ResetState();
            }

            if (session.RoundLogic is TowerFall.LastManStandingRoundLogic) //TODO: useful ?
            {
                var dynRoundLogicLM = DynamicData.For(session.RoundLogic);
                var dynRoundEndCounter = DynamicData.For(dynRoundLogicLM.Get<RoundEndCounter>("roundEndCounter"));
                dynRoundEndCounter.Set("endCounter", Constants.INITIAL_END_COUNTER);
                dynRoundLogicLM.Set("done", false);

                session.CurrentLevel.Ending = false;
            }
        }
    }
}
