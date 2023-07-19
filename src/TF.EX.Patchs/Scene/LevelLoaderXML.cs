using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models.State;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class LevelLoaderXMLPatch : IHookable
    {
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
