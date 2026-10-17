using HarmonyLib;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.State.Domain.Context;
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
        public static void Player_ctor(Player __instance, HatStates hatState)
        {
            var dynPlayer = DynamicData.For(__instance);
            dynPlayer.Add("isAimingRight", false);

            ApplySpawnHatRule(__instance, dynPlayer, hatState);
        }

        private static void ApplySpawnHatRule(Player player, DynamicData dynPlayer, HatStates requested)
        {
            var startsWithoutHat = SpawnHatRules.Get(player.PlayerIndex);

            if (requested != HatStates.Normal || startsWithoutHat == null)
            {
                return;
            }

            var target = startsWithoutHat.Value ? HatStates.NoHat : HatStates.Normal;

            if (player.HatState != target)
            {
                dynPlayer.Set("HatState", target);
                dynPlayer.Invoke("InitHead");
            }
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

        [HarmonyPrefix]
        [HarmonyPatch("LoseHat")]
        public static void Player_LoseHat_Prefix()
        {
            Calc.CalcPatch.RegisterRng();
        }

        [HarmonyPostfix]
        [HarmonyPatch("LoseHat")]
        public static void Player_LoseHat_Postfix()
        {
            Calc.CalcPatch.UnregisterRng();
        }

        [HarmonyPrefix]
        [HarmonyPatch("SpawnedInsideSolidCheck")]
        public static bool Player_SpawnedInsideSolidCheck_Prefix()
        {
            return !StateFlags.IsRestoring;
        }
    }
}
