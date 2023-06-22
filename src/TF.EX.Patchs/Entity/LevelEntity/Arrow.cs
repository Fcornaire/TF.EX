using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.State.Arrows;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class ArrowPatch : IHookable, IStateful<TowerFall.Arrow, Arrow>
    {
        private readonly INetplayManager _netplayManager;
        private readonly IArrowService _arrowService;

        public ArrowPatch(INetplayManager netplayManager, IArrowService arrowService)
        {
            _netplayManager = netplayManager;
            _arrowService = arrowService;
        }

        public void Load()
        {
            On.TowerFall.Arrow.EnterFallMode += Arrow_EnterFallMode;
            On.TowerFall.Arrow.DoWrapRender += Arrow_DoWrapRender;
            On.TowerFall.Arrow.Removed += Arrow_Removed;
            On.TowerFall.Arrow.EnforceLimit += Arrow_EnforceLimit;
        }

        public void Unload()
        {
            On.TowerFall.Arrow.EnterFallMode -= Arrow_EnterFallMode;
            On.TowerFall.Arrow.DoWrapRender -= Arrow_DoWrapRender;
            On.TowerFall.Arrow.Removed -= Arrow_Removed;
            On.TowerFall.Arrow.EnforceLimit -= Arrow_EnforceLimit;
        }

        //TODO: Properly track arrow decay to remove this
        private void Arrow_EnforceLimit(On.TowerFall.Arrow.orig_EnforceLimit orig, TowerFall.Level level)
        {
            //Console.WriteLine("Arrow EnforceLimit Ignore for now");
        }

        //TODO: remove this when a test without this is done
        private void Arrow_Removed(On.TowerFall.Arrow.orig_Removed orig, TowerFall.Arrow self)
        {
            orig(self);
            TowerFall.Arrow.FlushCache();
        }

        private void Arrow_DoWrapRender(On.TowerFall.Arrow.orig_DoWrapRender orig, TowerFall.Arrow self)
        {
            if (_netplayManager.IsTestMode() || _netplayManager.IsReplayMode())
            {
                self.DebugRender();
            }

            orig(self);
        }

        private void Arrow_EnterFallMode(On.TowerFall.Arrow.orig_EnterFallMode orig, TowerFall.Arrow self, bool bounce, bool zeroX, bool sound)
        {
            Calc.CalcPatch.RegisterRng();
            orig(self, bounce, zeroX, sound);
            Calc.CalcPatch.UnregisterRng();
        }

        public Arrow GetState(TowerFall.Arrow entity)
        {
            if (entity.StuckTo != null && entity.State == TowerFall.Arrow.ArrowStates.Stuck)
            {
                _arrowService.AddStuckArrow(entity.Position.ToModel(), entity.StuckTo);
            }

            var dynArrow = DynamicData.For(entity);
            var actualDepth = dynArrow.Get<double>("actualDepth");
            var shootingCounter = dynArrow.Get<Counter>("shootingCounter");
            var cannotPickupCounter = dynArrow.Get<Counter>("cannotPickupCounter");
            var cannotCatchCounter = dynArrow.Get<Counter>("cannotCatchCounter");
            var stuckDir = dynArrow.Get<Vector2>("stuckDir");
            var counter = dynArrow.Get<Vector2>("counter");
            var flashCounter = dynArrow.Get<float>("flashCounter");
            var flashInterval = dynArrow.Get<float>("flashInterval");

            return new Arrow
            {
                ActualDepth = actualDepth,
                ArrowType = entity.ArrowType.ToModel(),
                Direction = entity.Direction,
                PlayerIndex = entity.PlayerIndex,
                Position = entity.Position.ToModel(),
                ShootingCounter = shootingCounter.Value,
                CannotPickupCounter = cannotPickupCounter.Value,
                CannotCatchCounter = cannotCatchCounter.Value,
                Speed = entity.Speed.ToModel(),
                State = entity.State.ToModel(),
                StuckDirection = stuckDir.ToModel(),
                PositionCounter = counter.ToModel(),
                IsActive = entity.Active,
                IsCollidable = entity.Collidable,
                IsFrozen = entity.Frozen,
                IsVisible = entity.Visible,
                MarkedForRemoval = entity.MarkedForRemoval,
                Flash = new Flash(entity.Flashing, flashCounter, flashInterval),
                HasUnhittableEntity = entity.CannotHit != null
            };
        }

        public void LoadState(Arrow toLoad, TowerFall.Arrow entity)
        {
            var dynArrow = DynamicData.For(entity);
            dynArrow.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynArrow.Set("State", toLoad.State.ToTFModel());
            entity.Position = toLoad.Position.ToTFVector();
            entity.Direction = toLoad.Direction;
            dynArrow.Set("actualDepth", toLoad.ActualDepth);
            dynArrow.Set("PlayerIndex", toLoad.PlayerIndex);
            var shootingCounter = DynamicData.For(dynArrow.Get<Counter>("shootingCounter"));
            shootingCounter.Set("counter", toLoad.ShootingCounter);

            var cannotPickupCounter = DynamicData.For(dynArrow.Get<Counter>("cannotPickupCounter"));
            cannotPickupCounter.Set("counter", toLoad.CannotPickupCounter);

            var cannotCatchCounter = DynamicData.For(dynArrow.Get<Counter>("cannotCatchCounter"));
            cannotCatchCounter.Set("counter", toLoad.CannotCatchCounter);

            entity.Speed = toLoad.Speed.ToTFVector();
            dynArrow.Set("direction", toLoad.Direction);

            dynArrow.Set("StuckTo", _arrowService.GetPlatformStuck(toLoad.Position));
            dynArrow.Set("stuckDir", toLoad.StuckDirection.ToTFVector());
            entity.Collidable = toLoad.IsCollidable;
            entity.Active = toLoad.IsActive;

            dynArrow.Set("Frozen", toLoad.IsFrozen);
            entity.Visible = toLoad.IsVisible;

            dynArrow.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynArrow.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            dynArrow.Set("Flashing", toLoad.Flash.IsFlashing);
            dynArrow.Set("flashCounter", toLoad.Flash.FlashCounter);
            dynArrow.Set("flashInterval", toLoad.Flash.FlashInterval);
            dynArrow.Set("onFinish", () => { entity.RemoveSelf(); });

            if (!toLoad.HasUnhittableEntity)
            {
                entity.CannotHit = null;
            }
        }

        public void LoadCannotHit(TowerFall.Arrow self, bool hasUnhittable, int playerIndex)
        {
            if (hasUnhittable)
            {
                self.CannotHit = self.Level.GetPlayerOrCorpse(playerIndex);
            }
            else
            {
                self.CannotHit = null;
            }
        }

    }
}
