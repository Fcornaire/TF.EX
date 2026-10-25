using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.Extensions;
using TowerFall;
using static TowerFall.Player;


namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        private const float MINIMUM_DARKNESS_LEVEL = 0.5f;

        private static readonly BlendState BlendState = new() { ColorWriteChannels = ColorWriteChannels.None };

        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(int), typeof(Vector2), typeof(Allegiance), typeof(Allegiance), typeof(PlayerInventory), typeof(HatStates), typeof(bool), typeof(bool), typeof(bool) })]
        public static void Player_Ctor_Prefix(int playerIndex)
        {
            Domain.Models.Skin.SkinSlot.Enter(playerIndex);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(int), typeof(Vector2), typeof(Allegiance), typeof(Allegiance), typeof(PlayerInventory), typeof(HatStates), typeof(bool), typeof(bool), typeof(bool) })]
        public static void Player_Ctor_Finalizer()
        {
            Domain.Models.Skin.SkinSlot.Exit();
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Player_Update_Postfix(TowerFall.Player __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();

            PlayerInput.RightStickShot.UpdateIsAimingRight(__instance);

            // Kill a deconnected playe
            if (ExFlags.IsCaptureActive && inputService.GetCurrentInput(__instance.PlayerIndex).disconnected != 0 && !__instance.Dead)
            {
                __instance.Die(DeathCause.Curse, -1);
            }
        }

        public static void RefreshSprites(TowerFall.Level level)
        {
            var players = level.Players.OfType<TowerFall.Player>().ToList();

            if (players.Count == 0)
            {
                return;
            }

            var logger = ServiceCollections.ResolveLogger();
            var gameplayLayer = level.GetGameplayLayer();

            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState, gameplayLayer.SamplerState, DepthStencilState.None, RasterizerState.CullNone, gameplayLayer.Effect, Matrix.Lerp(Matrix.Identity, level.Camera.Matrix, gameplayLayer.CameraMultiplier));

            foreach (var player in players)
            {
                try
                {
                    player.DoWrapRender();
                }
                catch (Exception e)
                {
                    logger.LogDebug<PlayerPatch>($"Error when rendering player (may not cause issue ?) : {e.Message} => {e.StackTrace}");
                }
            }

            Draw.SpriteBatch.End();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DoWrapRender")]
        public static void Player_DoWrapRender_Prefix(TowerFall.Player __instance)
        {
            if (ExFlags.IsTestMode || (ExFlags.IsReplayMode && Engine.TFGamePatch.ShowHurtboxes))
            {
                __instance.DebugRender();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("HUDRender")]
        public static bool Player_HUDRender_Prefix(TowerFall.Player __instance)
        {
            if (!ExFlags.IsCaptureActive || __instance.Indicator == null || !ShouldHideIndicator(__instance))
            {
                return true;
            }

            __instance.ArrowHUD.Render();

            return false;
        }

        private static bool ShouldHideIndicator(TowerFall.Player player)
        {
            var isDarkEnough = player.Level?.LightingLayer?.Alpha >= MINIMUM_DARKNESS_LEVEL;

            return player.InvisOpacity < 1f || (isDarkEnough && player.LightAlpha < 1f);
        }
    }
}
