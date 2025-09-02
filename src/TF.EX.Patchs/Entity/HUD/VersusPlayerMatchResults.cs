using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    [HarmonyPatch(typeof(VersusPlayerMatchResults))]
    internal class VersusPlayerMatchResultsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session), typeof(TowerFall.VersusMatchResults), typeof(int), typeof(Vector2), typeof(Vector2), typeof(List<TowerFall.AwardInfo>)])]
        public static void VersusPlayerMatchResults_ctor(TowerFall.VersusPlayerMatchResults __instance, TowerFall.Session session, TowerFall.VersusMatchResults matchResults, int playerIndex, Vector2 tweenFrom, Vector2 tweenTo, List<TowerFall.AwardInfo> awards)
        {

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            var dynVersusPlayerMatchResults = DynamicData.For(__instance);
            var gem = dynVersusPlayerMatchResults.Get<Sprite<string>>("gem");

            var netplayIndex = playerIndex;

            //if (netplayManager.ShouldSwapPlayer())
            //{
            //    if (netplayIndex == 0)
            //    {
            //        netplayIndex = inputService.GetLocalPlayerInputIndex();
            //    }
            //    else
            //    {
            //        netplayIndex = inputService.GetRemotePlayerInputIndex();
            //    }
            //}

            var playerName = netplayIndex == 0 ? netplayManager.GetNetplayMeta().Name : netplayManager.GetPlayer2Name();

            var playerNameText = new OutlineText(TFGame.Font, playerName, gem.Position + Vector2.UnitY * 15);
            playerNameText.Color = Color.White;
            var dynPlayerNameText = DynamicData.For(playerNameText);
            dynPlayerNameText.Add("IsPlayerName", true);
            __instance.Add(playerNameText);
        }
    }
}
