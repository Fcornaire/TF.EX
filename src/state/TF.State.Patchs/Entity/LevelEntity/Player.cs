using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TowerFall;
using static TowerFall.Player;

namespace TF.State.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerStatePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(int), typeof(Vector2), typeof(Allegiance), typeof(Allegiance), typeof(PlayerInventory), typeof(HatStates), typeof(bool), typeof(bool), typeof(bool)])]
        public static void Player_ctor(Player __instance)
        {
            var dynPlayer = DynamicData.For(__instance);
            dynPlayer.Add("isAimingRight", false);
        }

        [HarmonyPrefix]
        [HarmonyPatch("DropArrow")]
        public static void Player_DropArrow_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("DropArrow")]
        public static void Player_DropArrow_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }
    }
}
