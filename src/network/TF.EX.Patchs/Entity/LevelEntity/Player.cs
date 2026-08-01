using HarmonyLib;
using TF.EX.Domain.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Context;
using TF.EX.Domain.Externals;
using TF.EX.Domain.Extensions;
using TowerFall;
using static TowerFall.Player;


namespace TF.EX.Patchs.Entity.LevelEntity
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        //Player_ctor and the DropArrow RNG bracket moved to
        //TF.State.Patchs.Entity.LevelEntity.PlayerStatePatch.

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void Player_Update_Postfix(TowerFall.Player __instance)
        {
            var inputService = ServiceCollections.ResolveInputService();
            var logger = ServiceCollections.ResolveLogger();
            var context = ServiceCollections.ResolveContext();

            if (ExFlags.IsCaptureActive)
            {
                // Kill a deconnected playe
                if (inputService.GetCurrentInput(__instance.PlayerIndex).disconnected != 0 && !__instance.Dead)
                {
                    __instance.Die(DeathCause.Curse, -1);
                    return;
                }

                var dynPlayer = DynamicData.For(__instance);

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

                var hasRightStickVariant = context.Registry.Variants.GetVariant(Constants.RIGHT_STICK_VARIANT_NAME) != null;

                if (!TowerFall.Player.ShootLock && hasRightStickVariant)
                {
                    var input = inputService.GetCurrentInput(__instance.PlayerIndex);

                    if (input.aim_right_axis.IsAfterThreshold())
                    {
                        if (!dynPlayer.Get<bool>("isAimingRight"))
                        {
                            ShootArrowWithRightStick(__instance);
                            dynPlayer.Set("isAimingRight", true);
                        }
                    }
                    else
                    {
                        dynPlayer.Set("isAimingRight", false);
                    }
                }
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

        private static void ShootArrowWithRightStick(Player self)
        {
            var inputService = ServiceCollections.ResolveInputService();

            var dynPlayer = Traverse.Create(self);
            var spamShotCounter = dynPlayer.Field("spamShotCounter").GetValue<Counter>();

            var input = inputService.GetCurrentInput(self.PlayerIndex);
            var rightAimDirection = TowerFall.PlayerInput.GetAimDirection(input.aim_right_axis.ToTFVector(), false);

            if (rightAimDirection.HasValue)
            {
                if (self.Arrows.HasArrows)
                {
                    if ((bool)spamShotCounter)
                    {
                        self.Level.Session.MatchStats[self.PlayerIndex].SpamShots++;
                    }
                    var currentAimDirection = self.AimDirection;

                    dynPlayer.Property("AimDirection").SetValue(rightAimDirection.Value);
                    var autoLockAngle = dynPlayer.Method("FindAutoLockAngle").GetValue<float>();
                    dynPlayer.Property("AimDirection").SetValue(currentAimDirection);

                    spamShotCounter.Set(10);
                    self.ArrowHUD.OnShoot();
                    self.ArcherData.SFX.FireArrow.Play(self.X);
                    Arrow arrow = Arrow.Create(self.Arrows.UseArrow(), self, self.Position + Player.ArrowOffset, autoLockAngle);
                    self.Level.Add(arrow);

                    //TODO: TriggerArrow
                    //if (arrow is TriggerArrow)
                    //{
                    //    RegisterTriggerArrow(arrow as TriggerArrow);
                    //}
                }
                else
                {
                    self.ArrowHUD.OnShootFail();
                    self.ArcherData.SFX.NoFire.Play(self.X);
                    TFGame.PlayerInputs[self.PlayerIndex].Rumble(0.25f, 5);
                    self.Level.Session.MatchStats[self.PlayerIndex].FailedShots++;
                    if (self.Level.Session.MatchSettings.Variants.CursedBows[self.PlayerIndex])
                    {
                        self.Speed = Monocle.Calc.AngleToVector(rightAimDirection.Value, -5.5f);
                        Sounds.sfx_cursedDeath.Play(self.X);
                        self.Die(DeathCause.Curse, self.PlayerIndex);
                    }
                }
            }
        }
    }
}
