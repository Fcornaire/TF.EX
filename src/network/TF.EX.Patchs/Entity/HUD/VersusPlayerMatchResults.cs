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
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session), typeof(TowerFall.VersusMatchResults), typeof(int), typeof(Vector2), typeof(Vector2), typeof(List<TowerFall.AwardInfo>)])]
        public static void VersusPlayerMatchResults_Ctor_Prefix(int playerIndex)
        {
            TF.EX.Domain.Models.Skin.SkinSlot.Enter(playerIndex);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session), typeof(TowerFall.VersusMatchResults), typeof(int), typeof(Vector2), typeof(Vector2), typeof(List<TowerFall.AwardInfo>)])]
        public static void VersusPlayerMatchResults_Ctor_Finalizer()
        {
            TF.EX.Domain.Models.Skin.SkinSlot.Exit();
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(TowerFall.Session), typeof(TowerFall.VersusMatchResults), typeof(int), typeof(Vector2), typeof(Vector2), typeof(List<TowerFall.AwardInfo>)])]
        public static void VersusPlayerMatchResults_ctor(TowerFall.VersusPlayerMatchResults __instance, TowerFall.Session session, TowerFall.VersusMatchResults matchResults, int playerIndex, Vector2 tweenFrom, Vector2 tweenTo, List<TowerFall.AwardInfo> awards)
        {

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var inputService = ServiceCollections.ResolveInputService();

            if (!netplayManager.IsInit() && !netplayManager.IsReplayMode() && !netplayManager.IsTestMode())
            {
                return;
            }

            var dynVersusPlayerMatchResults = DynamicData.For(__instance);
            var gem = dynVersusPlayerMatchResults.Get<Sprite<string>>("gem");

            // custom win/lose portraits can be larger than the vanilla 50px cell
            var portrait = dynVersusPlayerMatchResults.Get<Image>("portrait");

            if (portrait != null && portrait.Width > 50f)
            {
                portrait.Scale = Vector2.One * (50f / portrait.Width);
            }

            var playerName = netplayManager.GetNameForSeat(playerIndex);

            var playerNameText = new OutlineText(TFGame.Font, playerName, gem.Position + Vector2.UnitY * 15);
            playerNameText.Color = Color.White;
            var dynPlayerNameText = DynamicData.For(playerNameText);
            dynPlayerNameText.Add("IsPlayerName", true);
            __instance.Add(playerNameText);
        }
    }
}
