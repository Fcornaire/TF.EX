using HarmonyLib;
using TF.EX.Domain;
using TF.EX.Domain.Models.Skin;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(ArcherData))]
    public class ArcherDataPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ArcherData.Get), [typeof(int), typeof(ArcherData.ArcherTypes)])]
        public static void ArcherData_Get_Postfix(int characterIndex, ArcherData.ArcherTypes type, ref ArcherData __result)
        {
            var seat = SkinSlot.CurrentSeat;

            if (seat == null || __result == null)
            {
                return;
            }

            var skinned = ServiceCollections.ResolveSkinOverlayService().ResolveArcherSkinned(seat.Value, characterIndex, (int)type, __result);

            if (skinned != null)
            {
                __result = skinned;
            }
        }
    }
}
