//using HarmonyLib;
//using TF.EX.Domain;

//namespace TF.EX.Patchs.Background
//{
//    [HarmonyPatch(typeof(TowerFall.Background.FadeLayer))]
//    public class FadeLayerPatch
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch("UpdatePosition")]
//        public static bool FadeLayer_UpdatePosition(TowerFall.Background.FadeLayer __instance)
//        {
//            var netplayManager = ServiceCollections.ResolveNetplayManager();
//            if (netplayManager.IsInit())
//            {
//                __instance.Sprite.Position = __instance.Position - __instance.Range;
//                __instance.Sprite.Rate = 0.6f;
//                return false;
//            }

//            return true;
//        }
//    }
//}
