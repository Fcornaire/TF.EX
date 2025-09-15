//using HarmonyLib;
//using TowerFall;

//namespace TF.EX.Patchs.Entity.MenuItem
//{
//    [HarmonyPatch(typeof(FightButton))]
//    public class FightButtonPatch
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch("MenuAction")]
//        public static bool MenuAction_Prefix()
//        {
//            (Monocle.Engine.Instance.Scene as MainMenu).State = MainMenu.MenuState.VersusOptions;
//            return false;
//        }
//    }

//}
