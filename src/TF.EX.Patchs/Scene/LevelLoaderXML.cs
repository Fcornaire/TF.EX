using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.CustomComponent;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Scene
{
    [HarmonyPatch(typeof(LevelLoaderXML))]
    public class LevelLoaderXMLPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void LevelLoaderXML_Update(LevelLoaderXML __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (__instance.Finished
                && TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel().IsNetplay()
                && !netplayManager.IsReplayMode()
                && !netplayManager.IsSynchronized()
                && __instance.Level.Session.RoundIndex == 0
                )
            {
                Notification.Create(__instance.Level, "Waiting for other players...", 20, 0, false, true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session)])]
        public static void LevelLoaderXML_ctor(TowerFall.Session session)
        {
            Reset(session);
        }

        /// <summary>
        /// Reset for next round
        /// </summary>
        /// <param name="session"></param>
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
                dynRoundLogicLM.Set("done", false);

                session.CurrentLevel.Ending = false;
            }
        }
    }
}
