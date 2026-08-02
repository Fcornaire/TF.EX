using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;

using TF.State.Domain.Context;
namespace TF.State.Patchs.Background
{
    [HarmonyPatch(typeof(TowerFall.Background))]
    internal class BackgroundPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool Background_Update(TowerFall.Background __instance)
        {

            if (!StateFlags.IsRollbackFrame && !StateFlags.HasFramesToReSimulate)
            {
                return true;
            }

            if (TowerFall.TFGame.Instance?.Scene is TowerFall.Level level && !level.Frozen)
            {
                return false;
            }

            return true;
        }
    }
}
