using HarmonyLib;
using TF.State.Domain.Context;

namespace TF.State.Patchs.Entity.LevelEntity
{
    //Useful to feeze untracked entity to not make them move at the speed of light during rollbak
    public static class CosmeticFreeze
    {
        public static bool ShouldFreeze => StateFlags.IsRollbackFrame || StateFlags.HasFramesToReSimulate;

        private static readonly List<Monocle.Component> _frozen = new List<Monocle.Component>();

        public static void Freeze(Monocle.Entity entity)
        {
            Unfreeze();

            if (!ShouldFreeze || entity?.Components == null)
            {
                return;
            }

            var components = entity.Components;

            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];

                if (component is Monocle.Coroutine || !component.Active)
                {
                    continue;
                }

                component.Active = false;
                _frozen.Add(component);
            }
        }

        public static void Unfreeze()
        {
            for (int i = 0; i < _frozen.Count; i++)
            {
                _frozen[i].Active = true;
            }

            _frozen.Clear();
        }
    }

    [HarmonyPatch(typeof(TowerFall.Lantern))]
    internal class LanternCosmeticPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void Lantern_Update_Prefix(TowerFall.Lantern __instance) => CosmeticFreeze.Freeze(__instance);

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Lantern_Update_Postfix() => CosmeticFreeze.Unfreeze();
    }

    [HarmonyPatch(typeof(TowerFall.BombParticle))]
    internal class BombParticlePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool BombParticle_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.PirateBanner))]
    internal class PirateBannerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool PirateBanner_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.DreadBGTentacle))]
    internal class DreadBGTentaclePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool DreadBGTentacle_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.YellowArcherStatue))]
    internal class YellowArcherStatuePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool YellowArcherStatue_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.BGCrystal))]
    internal class BGCrystalPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool BGCrystal_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.CataclysmMeat))]
    internal class CataclysmMeatPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool CataclysmMeat_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.HeavyPhysicsObject))]
    internal class HeavyPhysicsObjectPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool HeavyPhysicsObject_Update() => !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.LevelEntity))]
    internal class ShockCirclePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool LevelEntity_Update(TowerFall.LevelEntity __instance) => __instance is not TowerFall.ShockCircle || !CosmeticFreeze.ShouldFreeze;
    }

    [HarmonyPatch(typeof(TowerFall.PlayerHair))]
    internal class PlayerHairPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static bool PlayerHair_Update() => !CosmeticFreeze.ShouldFreeze;
    }

}
