using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;


namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class PlayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;

        public PlayerPatch(INetplayManager netplayManager, IInputService inputService)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
        }

        public void Load()
        {
            On.TowerFall.Player.DoWrapRender += Player_DoWrapRender;
            On.TowerFall.Player.Update += Player_Update;
            On.TowerFall.Player.ctor += Player_ctor;
        }

        public void Unload()
        {
            On.TowerFall.Player.DoWrapRender -= Player_DoWrapRender;
            On.TowerFall.Player.Update -= Player_Update;
            On.TowerFall.Player.ctor -= Player_ctor;
        }

        private void Player_ctor(On.TowerFall.Player.orig_ctor orig, TowerFall.Player self, int playerIndex, Vector2 position, Allegiance allegiance, Allegiance teamColor, TowerFall.PlayerInventory inventory, TowerFall.Player.HatStates hatState, bool frozen, bool flash, bool indicator)
        {
            orig(self, playerIndex, position, allegiance, teamColor, inventory, hatState, frozen, flash, indicator);

            var dynPlayer = DynamicData.For(self);
            dynPlayer.Add("isAimingRight", false);
        }

        private void Player_Update(On.TowerFall.Player.orig_Update orig, TowerFall.Player self)
        {
            orig(self);

            if (_netplayManager.IsInit() && !TowerFall.Player.ShootLock && VariantManager.GetCustomVariant(Constants.RIGHT_STICK_VARIANT_NAME))
            {
                var playerIndex = self.PlayerIndex;
                if (_netplayManager.ShouldSwapPlayer())
                {
                    if (playerIndex == 0)
                    {
                        playerIndex = _inputService.GetLocalPlayerInputIndex();
                    }
                    else
                    {
                        playerIndex = _inputService.GetRemotePlayerInputIndex();
                    }
                }

                var input = _inputService.GetCurrentInput(playerIndex);
                var dynPlayer = DynamicData.For(self);

                if (input.aim_right_axis.IsAfterThreshold())
                {
                    if (!dynPlayer.Get<bool>("isAimingRight"))
                    {
                        ShootArrowWithRightStick(self);
                        dynPlayer.Set("isAimingRight", true);
                    }
                }
                else
                {
                    dynPlayer.Set("isAimingRight", false);
                }
            }
        }

        private void Player_DoWrapRender(On.TowerFall.Player.orig_DoWrapRender orig, TowerFall.Player self)
        {
            if (_netplayManager.IsTestMode() || _netplayManager.IsReplayMode())
            {
                self.DebugRender();
            }
            orig(self);
        }

        private void ShootArrowWithRightStick(Player self)
        {
            var dynPlayer = DynamicData.For(self);
            Counter spamShotCounter = dynPlayer.Get<Counter>("spamShotCounter");

            var playerIndex = self.PlayerIndex;
            if (_netplayManager.ShouldSwapPlayer())
            {
                if (playerIndex == 0)
                {
                    playerIndex = _inputService.GetLocalPlayerInputIndex();
                }
                else
                {
                    playerIndex = _inputService.GetRemotePlayerInputIndex();
                }
            }

            var input = _inputService.GetCurrentInput(playerIndex);
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

                    dynPlayer.Set("AimDirection", rightAimDirection.Value);
                    var autoLockAngle = dynPlayer.Invoke<float>("FindAutoLockAngle");
                    dynPlayer.Set("AimDirection", currentAimDirection);

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
