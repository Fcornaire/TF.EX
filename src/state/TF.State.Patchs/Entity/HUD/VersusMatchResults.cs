using HarmonyLib;
using MonoMod.Utils;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.HUD
{
    [HarmonyPatch(typeof(VersusMatchResults))]
    public class VersusMatchResultsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Session), typeof(VersusRoundResults)])]
        public static void VersusMatchResults_ctor(VersusMatchResults __instance, Session session)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            var rngService = ServiceCollections.ResolveRngService();

            (TFGame.Instance.Scene as Level).Frozen = true;

            var dynVersusMatchResults = DynamicData.For(__instance);
            dynVersusMatchResults.Add("HasReset", false);

            session.MatchSettings.RandomLevelSeed = rngService.GetSeed();

            var entity = new VersusSeedDisplay(session.MatchSettings.RandomSeedIcons);
            session.CurrentLevel.Layers[entity.LayerIndex].Entities.Add(entity);
        }
    }
}
