using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TF.State.Domain.Models;
using TF.State.TowerFallExtensions;
using TowerFall;

namespace TF.State.Patchs.Scene
{
    [HarmonyPatch(typeof(LevelLoaderXML))]
    public class LevelLoaderXMLStatePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session)])]
        public static void LevelLoaderXML_ctor(TowerFall.Session session)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            Reset(session);

            if (session.TreasureSpawner != null)
            {
                Entity.TreasureSpawnerPatch.UseDeterministRandom(session.TreasureSpawner);
            }

            if (session.MatchSettings.LevelSystem is VersusLevelSystem versusLevelSystem)
            {
                var dynLevelSystem = Traverse.Create(versusLevelSystem);
                dynLevelSystem.Property("ShowControls").SetValue(false);
                dynLevelSystem.Property("ShowTriggerControls").SetValue(false);
            }
        }

        private static void Reset(TowerFall.Session session)
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
                dynRoundEndCounter.Set("ghostWaitCounter", Constants.INITIAL_END_COUNTER);
                dynRoundLogicLM.Set("done", false);

                session.CurrentLevel.Ending = false;
            }
        }
    }
}
