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
            var logger = ServiceCollections.ResolveLogger();

            PlayerInput.RightStickShot.UpdateIsAimingRight(__instance);

            if (ExFlags.IsCaptureActive)
            {
                // Kill a deconnected playe
                if (inputService.GetCurrentInput(__instance.PlayerIndex).disconnected != 0 && !__instance.Dead)
                {
                    __instance.Die(DeathCause.Curse, -1);
                    return;
                }

                // Sprite update are done in the DoWrapRender method, so we manually call it here to have it in the game state
                var gameplayLayer = __instance.Level.GetGameplayLayer();
                var blendState = gameplayLayer.BlendState;
                var samplerState = gameplayLayer.SamplerState;
                var effect = gameplayLayer.Effect;
                var cameraMultiplier = gameplayLayer.CameraMultiplier;

                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, blendState, samplerState, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Lerp(Matrix.Identity, __instance.Scene.Camera.Matrix, cameraMultiplier));
                try
                {
                    __instance.DoWrapRender();
                }
                catch (Exception e)
                {
                    logger.LogDebug<PlayerPatch>($"Error when rendering player (may not cause issue ?) : {e.Message} => {e.StackTrace}");
                }
                Draw.SpriteBatch.End();
            }
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
    }
}
