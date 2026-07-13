using HarmonyLib;
using Microsoft.Xna.Framework;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(TreasureSpawner))]
    public class TreasureSpawnerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(Session), typeof(int[]), typeof(float), typeof(bool)])]
        public static void TreasureSpawner_ctor_Session_Int32Array_float_bool(TreasureSpawner __instance)
        {
            Traverse.Create(__instance).Property("Random").SetValue(Monocle.Calc.Random);
            //var dynSpawner = MonoMod.Utils.DynamicData.For(__instance);
            //dynSpawner.Set("Random", Monocle.Calc.Random);
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetChestSpawnsForLevel")]
        public static void TreasureSpawner_GetChestSpawnsForLevel_Prefix(List<Vector2> chestPositions, ref List<Vector2> bigChestPositions)
        {
            Calc.CalcPatch.RegisterRng();
            Calc.CalcPatch.RegisterShuffle(chestPositions);
            bigChestPositions = new List<Vector2>(); //TODO: re enable big chests
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetChestSpawnsForLevel")]
        public static void TreasureSpawner_GetChestSpawnsForLevel_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();

            //if (res.Count > 0) //Useful for test only
            //{
            //    foreach (var c in res.ToArray())
            //    {
            //        var dynPickup = MonoMod.Utils.DynamicData.For(c);
            //        dynPickup.Set("pickups", new List<TowerFall.Pickups> { TowerFall.Pickups.LavaOrb });
            //    }
            //}
        }
    }
}
