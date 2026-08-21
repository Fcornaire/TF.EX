using FortRise;
using HarmonyLib;
using TF.State.Domain;
using TF.State.Domain.Context;
using TowerFall;
namespace TF.State.Patchs.Entity
{
    [HarmonyPatch(typeof(TreasureSpawner))]
    public class TreasureSpawnerPatch
    {
        public static void UseDeterministRandom(TreasureSpawner spawner)
        {
            Traverse.Create(spawner).Property("Random").SetValue(ServiceCollections.ResolveRngService().Gameplay);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TreasureSpawner.GetRandomArrowType))]
        public static bool TreasureSpawner_GetRandomArrowType_Prefix(TreasureSpawner __instance, bool includeDefaultArrows, ref ArrowTypes __result)
        {
            if (!StateFlags.IsCaptureActive)
            {
                return true;
            }

            var arrowTypes = new List<ArrowTypes> { __instance.orig_GetRandomArrowType(includeDefaultArrows) };

            foreach (var entry in PickupsRegistry.GetAllPickups().Values)
            {
                if (entry.Configuration.ArrowType.TryGetValue(out var arrowType)
                    && !__instance.Exclusions.Contains(entry.Pickups)
                    && IsModuleSafe(entry))
                {
                    arrowTypes.Add(arrowType);
                }
            }

            __result = Monocle.Calc.Choose(__instance.Random, arrowTypes);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TreasureSpawner.GetArrowShufflePickups))]
        public static void TreasureSpawner_GetArrowShufflePickups_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TreasureSpawner.GetArrowShufflePickups))]
        public static void TreasureSpawner_GetArrowShufflePickups_Postfix(List<Pickups> __result)
        {
            Calc.CalcPatch.UnregisterRng();

            if (!StateFlags.IsCaptureActive)
            {
                return;
            }

            __result.RemoveAll(pickup => !IsModuleSafe(PickupsRegistry.GetPickup(pickup)));
        }

        private static bool IsModuleSafe(IPickupEntry entry)
        {
            if (entry == null)
            {
                return true;
            }

            var separator = entry.Name.IndexOf('/');

            return separator > 0 && ServiceCollections.ResolveAPIManager().IsModuleSafe(entry.Name.Substring(0, separator));
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetChestSpawnsForLevel")]
        public static void TreasureSpawner_GetChestSpawnsForLevel_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
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
