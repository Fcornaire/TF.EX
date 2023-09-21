using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    public class LevelLoaderXMLPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public LevelLoaderXMLPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.LevelLoaderXML.ctor += LevelLoaderXML_ctor;
            On.TowerFall.LevelLoaderXML.Update += LevelLoaderXML_Update;
        }

        public void Unload()
        {
            On.TowerFall.LevelLoaderXML.ctor -= LevelLoaderXML_ctor;
            On.TowerFall.LevelLoaderXML.Update -= LevelLoaderXML_Update;
        }

        private void LevelLoaderXML_Update(On.TowerFall.LevelLoaderXML.orig_Update orig, LevelLoaderXML self)
        {
            orig(self);

            if (self.Finished
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && !_netplayManager.IsReplayMode()
                && !_netplayManager.IsSynchronized()
                && self.Level.Get<Dialog>() is null
                && self.Level.Session.RoundIndex == 0
                )
            {
                var dialog = new Dialog("Infos",
                    "Establishing a connection ...",
                    new Vector2(160f, 120f),
                    null,
                    new Dictionary<string, Action>(),
                    null,
                    true
                );
                var dynLayer = DynamicData.For(self.Level.GetGameplayLayer());
                dynLayer.Invoke("Add", dialog, false);
            }
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
            TFGame.Instance.Screen.Offset = Vector2.Zero;

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
