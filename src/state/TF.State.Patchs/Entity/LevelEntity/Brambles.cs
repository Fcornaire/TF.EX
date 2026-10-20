using HarmonyLib;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Brambles))]
    public class BramblesPatch
    {
        private static readonly AccessTools.FieldRef<Stack<Brambles>> Cached =
            AccessTools.StaticFieldRefAccess<Stack<Brambles>>(AccessTools.Field(typeof(Brambles), "cached"));

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Brambles.Create))]
        public static void Create_Prefix()
        {
            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            Cached().Clear();
        }
    }
}
