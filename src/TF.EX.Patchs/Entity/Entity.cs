using HarmonyLib;
using MonoMod.Utils;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity
{
    [HarmonyPatch(typeof(Monocle.Entity))]
    internal class EntityPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Removed")]
        public static void Entity_Removed(Monocle.Entity __instance)
        {
            if (__instance is TowerFall.Lava) //Hack !!
            {
                Traverse.Create(__instance).Property("Scene").SetValue(TowerFall.TFGame.Instance.Scene as Level); //TODO: Remove this hack, we should have a scene here, dunno why it's null
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("RemoveSelf")]
        public static bool Entity_RemoveSelf(Monocle.Entity __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            if (!netplayManager.IsUpdating()) //This is mainly to prevent arrowCushion deleting arrow on RBF but should be usefull for all entities/scenario
            {
                if (__instance is Explosion)  //Hack because cache i think
                {
                    var dynExplosion = DynamicData.For(__instance);
                    dynExplosion.Set("Scene", TowerFall.TFGame.Instance.Scene);
                    dynExplosion.Set("Level", TowerFall.TFGame.Instance.Scene as Level);
                }

                return true;
            }

            return false;
        }
    }
}
