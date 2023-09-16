using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System.Reflection;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class PlayerExtensions
    {
        public static Player GetState(this TowerFall.Player entity)
        {
            var dynPlayer = DynamicData.For(entity);
            var stateMachine = entity.GetComponent<StateMachine>();

            var dynScheduler = DynamicData.For(dynPlayer.Get<Monocle.Scheduler>("scheduler"));
            var schedulerCountersCopy = new List<float>();
            schedulerCountersCopy.Copy(dynScheduler.Get<List<float>>("counters"));
            var schedulerStartCounters = new List<int>();
            schedulerStartCounters.Copy(dynScheduler.Get<List<int>>("startCounters"));

            var schedulerState = new TF.EX.Domain.Models.State.Entity.LevelEntity.Player.Scheduler
            {
                SchedulerActions = dynScheduler.Get<List<Action>>("actions").ToNames(),
                SchedulerCounters = schedulerCountersCopy,
                SchedulerStartCounters = schedulerStartCounters,
            };

            var actualDepth = dynPlayer.Get<double>("actualDepth");
            var deathArrow = dynPlayer.Get<TowerFall.Arrow>("deathArrow");
            var deathArrowActualDepth = deathArrow != null ? DynamicData.For(deathArrow).Get<double>("actualDepth") : -1;

            var shield = dynPlayer.Get<TowerFall.PlayerShield>("shield");
            var wings = dynPlayer.Get<TowerFall.PlayerWings>("wings");

            double lastPlaformDepth = -1;
            var lastPlatform = dynPlayer.Get<TowerFall.Platform>("lastPlatform");
            if (lastPlatform != null)
            {
                var dynLastPlatform = DynamicData.For(lastPlatform);

                lastPlaformDepth = dynLastPlatform.Get<double>("actualDepth");
            }

            var body = dynPlayer.Get<Monocle.Sprite<string>>("bodySprite");
            var head = dynPlayer.Get<Monocle.Sprite<string>>("headSprite");
            var headBack = dynPlayer.Get<Monocle.Sprite<string>>("headBackSprite");
            var bow = dynPlayer.Get<Monocle.Sprite<string>>("bowSprite");
            var shieldSprite = DynamicData.For(shield).Get<Monocle.SpritePart<int>>("sprite");
            var wingsSprite = DynamicData.For(wings).Get<Monocle.Sprite<string>>("sprite");

            var playerAnimations = new PlayerAnimations
            {
                Body = body.GetState(),
                Head = head.GetState(),
                HeadBack = headBack != null ? headBack.GetState() : null,
                Bow = bow.GetState(),
                Shield = shield != null ? shieldSprite.GetState() : null,
                Wings = wings != null ? wingsSprite.GetState() : null
            };

            var lastAimDirection = dynPlayer.Get<float>("lastAimDirection");

            return new TF.EX.Domain.Models.State.Entity.LevelEntity.Player.Player
            {
                MarkedForRemoval = entity.MarkedForRemoval,
                ActualDepth = actualDepth,
                DeathArrowDepth = deathArrowActualDepth,
                IsCollidable = entity.Collidable,
                IsDead = entity.Dead,
                Position = entity.Position.ToModel(),
                PositionCounter = dynPlayer.Get<Vector2>("counter").ToModel(),
                Facing = (int)entity.Facing,
                WallStickMax = dynPlayer.Get<float>("wallStickMax"),
                ArrowsInventory = entity.Arrows.Arrows.ToModel(),
                AutoMove = dynPlayer.Get<int>("autoMove"),
                Speed = entity.Speed.ToModel(),
                FlapGravity = dynPlayer.Get<float>("flapGravity"),
                CanHyper = dynPlayer.Get<bool>("canHyper"),
                JumpBufferCounter = dynPlayer.Get<Counter>("jumpBufferCounter").Value,
                DodgeEndCounter = dynPlayer.Get<Counter>("dodgeEndCounter").Value,
                JumpGraceCounter = dynPlayer.Get<Counter>("jumpGraceCounter").Value,
                DodgeCatchCounter = dynPlayer.Get<Counter>("dodgeCatchCounter").Value,
                DyingCounter = dynPlayer.Get<Counter>("dyingCounter").Value,
                FlapBounceCounter = dynPlayer.Get<Counter>("flapBounceCounter").Value,
                DodgeSlide = new DodgeSlide(entity.DodgeSliding, dynPlayer.Get<bool>("wasDodgeSliding")),
                Hitbox = GetHitbox(entity),
                DodgeStallCounter = dynPlayer.Get<Counter>("dodgeStallCounter").Value,
                DodgeCooldown = dynPlayer.Get<bool>("dodgeCooldown"),
                Aiming = entity.Aiming,
                CanVarJump = dynPlayer.Get<bool>("canVarJump"),
                IsOnGround = entity.OnGround,
                DuckSlipCounter = dynPlayer.Get<float>("duckSlipCounter"),
                State = new PlayerState(
                    ((TowerFall.Player.PlayerStates)stateMachine.State).ToModel(),
                    ((TowerFall.Player.PlayerStates)stateMachine.PreviousState).ToModel()
                ),
                Index = entity.PlayerIndex,
                Scheduler = schedulerState,
                IsShieldVisible = shield.Visible,
                IsWingsVisible = wings.Visible,
                Flash = new Flash(entity.Flashing, dynPlayer.Get<float>("flashCounter"), dynPlayer.Get<float>("flashInterval")),
                ShouldDrawSelf = entity.DrawSelf,
                ShouldStartAimingDown = dynPlayer.Get<bool>("startAimingDown"),
                GraceLedgeDir = dynPlayer.Get<int>("graceLedgeDir"),
                LastPlatformDepth = lastPlaformDepth,
                Animations = playerAnimations,
                Cling = entity.Cling,
                LastAimDir = lastAimDirection,
                HasSpeedBoots = entity.HasSpeedBoots,
                IsInvisible = entity.Invisible
            };
        }

        /// <summary>
        /// Used to load game state info into the actual game
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerToLoad"></param>
        public static void LoadState(this TowerFall.Player entity, Player toLoad)
        {
            var dynPlayer = DynamicData.For(entity);

            if (entity.Scene == null)
            {
                dynPlayer.Set("Scene", Monocle.Engine.Instance.Scene);
                entity.Added();
            }

            var stateMachine = entity.GetComponent<StateMachine>();

            dynPlayer.Set("actualDepth", toLoad.ActualDepth);
            entity.Collidable = toLoad.IsCollidable;
            dynPlayer.Set("dead", toLoad.IsDead);
            entity.Position = toLoad.Position.ToTFVector();
            dynPlayer.Set("counter", toLoad.PositionCounter.ToTFVector());
            dynPlayer.Set("Facing", (TowerFall.Facing)toLoad.Facing);
            dynPlayer.Set("wallStickMax", toLoad.WallStickMax);
            entity.Arrows.Arrows.Update(toLoad.ArrowsInventory);
            dynPlayer.Set("autoMove", toLoad.AutoMove);
            entity.Speed = toLoad.Speed.ToTFVector();
            dynPlayer.Set("flapGravity", toLoad.FlapGravity);
            dynPlayer.Set("canHyper", toLoad.CanHyper);

            var jumpBufferCounter = DynamicData.For(dynPlayer.Get<Counter>("jumpBufferCounter"));
            jumpBufferCounter.Set("counter", toLoad.JumpBufferCounter);
            var dodgeEndCounter = DynamicData.For(dynPlayer.Get<Counter>("dodgeEndCounter"));
            dodgeEndCounter.Set("counter", toLoad.DodgeEndCounter);
            var jumpGraceCounter = DynamicData.For(dynPlayer.Get<Counter>("jumpGraceCounter"));
            jumpGraceCounter.Set("counter", toLoad.JumpGraceCounter);
            var dodgeCatchCounter = DynamicData.For(dynPlayer.Get<Counter>("dodgeCatchCounter"));
            dodgeCatchCounter.Set("counter", toLoad.DodgeCatchCounter);
            var dyingCounter = DynamicData.For(dynPlayer.Get<Counter>("dyingCounter"));
            dyingCounter.Set("counter", toLoad.DyingCounter);
            var flapBounceCounter = DynamicData.For(dynPlayer.Get<Counter>("flapBounceCounter"));
            flapBounceCounter.Set("counter", toLoad.FlapBounceCounter);

            dynPlayer.Set("DodgeSliding", toLoad.DodgeSlide.IsDodgeSliding);
            dynPlayer.Set("wasDodgeSliding", toLoad.DodgeSlide.WasDodgeSliding);
            LoadHitbox(entity, toLoad.Hitbox);

            var dodgeStallCounter = DynamicData.For(dynPlayer.Get<Counter>("dodgeStallCounter"));
            dodgeStallCounter.Set("counter", toLoad.DodgeStallCounter);

            dynPlayer.Set("dodgeCooldown", toLoad.DodgeCooldown);
            dynPlayer.Set("aiming", toLoad.Aiming);
            dynPlayer.Set("canVarJump", toLoad.CanVarJump);
            dynPlayer.Set("OnGround", toLoad.IsOnGround);
            dynPlayer.Set("duckSlipCounter", toLoad.DuckSlipCounter);

            var dynStateMachine = DynamicData.For(stateMachine);
            dynStateMachine.Set("state", (int)toLoad.State.CurrentState.ToTFModel());
            dynStateMachine.Set("PreviousState", (int)toLoad.State.PreviousState.ToTFModel());

            var dynShield = DynamicData.For(dynPlayer.Get<TowerFall.PlayerShield>("shield"));
            dynShield.Set("Visible", toLoad.IsShieldVisible);

            dynPlayer.Set("DrawSelf", toLoad.ShouldDrawSelf);
            dynShield.Set("Active", toLoad.IsShieldVisible);

            var dynWings = DynamicData.For(dynPlayer.Get<TowerFall.PlayerWings>("wings"));
            if (dynWings.Get<bool>("Visible") || dynWings.Get<bool>("Active"))
            {
                TowerFall.Sounds.pu_wingFly.Stop();
                TowerFall.Sounds.pu_wing.Stop();
            }

            dynWings.Set("Visible", toLoad.IsWingsVisible);
            dynWings.Set("Active", toLoad.IsWingsVisible);

            dynPlayer.Set("Flashing", toLoad.Flash.IsFlashing);
            dynPlayer.Set("flashCounter", toLoad.Flash.FlashCounter);
            dynPlayer.Set("flashInterval", toLoad.Flash.FlashInterval);

            dynPlayer.Set("startAimingDown", toLoad.ShouldStartAimingDown);
            dynPlayer.Set("graceLedgeDir", toLoad.GraceLedgeDir);
            dynPlayer.Set("MarkedForRemoval", toLoad.MarkedForRemoval);

            var dynScheduler = DynamicData.For(dynPlayer.Get<Monocle.Scheduler>("scheduler"));

            if (toLoad.Scheduler.SchedulerActions != null)
            {
                dynScheduler.Set("actions", ToActions(entity, toLoad.Scheduler.SchedulerActions));
            }
            if (toLoad.Scheduler.SchedulerCounters != null)
            {
                var counters = dynScheduler.Get<List<float>>("counters");
                counters.Clear();
                counters.Copy(toLoad.Scheduler.SchedulerCounters);
            }
            if (toLoad.Scheduler.SchedulerStartCounters != null)
            {
                var startCounters = dynScheduler.Get<List<int>>("startCounters");
                startCounters.Clear();
                startCounters.Copy(toLoad.Scheduler.SchedulerStartCounters);
            }

            var platforms = entity.Level.GetAll<TowerFall.Platform>().ToArray();
            var foundPlat = false;
            foreach (var plat in platforms)
            {
                var dynPlat = DynamicData.For(plat);
                var actualDepth = dynPlat.Get<double>("actualDepth");

                if (actualDepth == toLoad.LastPlatformDepth)
                {
                    dynPlayer.Set("lastPlatform", plat);
                    foundPlat = true;
                    break;
                }
            }
            if (!foundPlat)
            {
                dynPlayer.Set("lastPlatform", null);
            }

            if (dynShield.Get<bool>("Visible"))
            {
                entity.TargetCollider = dynPlayer.Get<TowerFall.WrapHitbox>("shieldHitbox");
            }
            else
            {
                entity.TargetCollider = null;
            }

            var body = dynPlayer.Get<Monocle.Sprite<string>>("bodySprite");
            var head = dynPlayer.Get<Monocle.Sprite<string>>("headSprite");
            var headBack = dynPlayer.Get<Monocle.Sprite<string>>("headBackSprite");
            var bow = dynPlayer.Get<Monocle.Sprite<string>>("bowSprite");
            var shieldSprite = dynShield.Get<Monocle.SpritePart<int>>("sprite");
            var wingsSprite = dynWings.Get<Monocle.Sprite<string>>("sprite");

            body.LoadState(toLoad.Animations.Body);
            head.LoadState(toLoad.Animations.Head);
            if (headBack != null)
            {
                headBack.LoadState(toLoad.Animations.HeadBack);
            }
            bow.LoadState(toLoad.Animations.Bow);
            if (shieldSprite != null)
            {
                shieldSprite.LoadState(toLoad.Animations.Shield);
            }
            if (wingsSprite != null)
            {
                wingsSprite.LoadState(toLoad.Animations.Wings);
            }

            dynPlayer.Set("Cling", toLoad.Cling);
            dynPlayer.Set("lastAimDirection", toLoad.LastAimDir);
            entity.HasSpeedBoots = toLoad.HasSpeedBoots;
            entity.Invisible = toLoad.IsInvisible;
        }

        public static void LoadDeathArrow(this TowerFall.Player self, double deathArrowActualDepth)
        {
            var dynPlayer = DynamicData.For(self);

            if (deathArrowActualDepth != -1)
            {
                dynPlayer.Set("deathArrow", self.GetTFArrowByDepth(deathArrowActualDepth));
            }
            else
            {
                dynPlayer.Set("deathArrow", null);
            }
        }

        private static TowerFall.Arrow GetTFArrowByDepth(this TowerFall.Player self, double actualDepth)
        {
            var arrows = self.Scene[Monocle.GameTags.Arrow].ToArray();

            TowerFall.Arrow arrow = null;

            foreach (var entity in arrows)
            {
                var dynArrow = DynamicData.For(entity);
                var depth = dynArrow.Get<double>("actualDepth");

                if (depth == actualDepth)
                {
                    arrow = (TowerFall.Arrow)entity;
                    break;
                }
            }

            return arrow;
        }

        private static Domain.Models.State.Entity.LevelEntity.Player.Hitbox GetHitbox(TowerFall.Player self)
        {
            var dynPlayer = DynamicData.For(self);
            var normalHitbox = dynPlayer.Get<TowerFall.WrapHitbox>("normalHitbox");
            if (self.Collider == normalHitbox)
            {
                return Domain.Models.State.Entity.LevelEntity.Player.Hitbox.Normal;
            }

            return Domain.Models.State.Entity.LevelEntity.Player.Hitbox.Ducking;
        }

        private static void LoadHitbox(TowerFall.Player self, Domain.Models.State.Entity.LevelEntity.Player.Hitbox hitbox)
        {
            var dynPlayer = DynamicData.For(self);

            switch (hitbox)
            {
                case Domain.Models.State.Entity.LevelEntity.Player.Hitbox.Normal:
                    dynPlayer.Invoke("UseNormalHitbox");
                    break;
                case Domain.Models.State.Entity.LevelEntity.Player.Hitbox.Ducking:
                    dynPlayer.Invoke("UseDuckingHitbox");
                    break;
            }
        }

        private static List<Action> ToActions(TowerFall.Player self, List<string> actions)
        {
            List<Action> list = new List<Action>();
            var dynPlayer = DynamicData.For(self);

            foreach (var action in actions.ToArray())
            {
                switch (action)
                {
                    case "DodgeCooldown":
                        var dodgeCooldown = typeof(TowerFall.Player).GetMethod("DodgeCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
                        Action dodgeCooldownAction = (Action)Delegate.CreateDelegate(typeof(Action), self, dodgeCooldown);

                        list.Add(dodgeCooldownAction);
                        break;
                    case "FinishAutoMove":
                        var finishAutoMove = typeof(TowerFall.Player).GetMethod("FinishAutoMove", BindingFlags.NonPublic | BindingFlags.Instance);
                        Action finishAutoMoveAction = (Action)Delegate.CreateDelegate(typeof(Action), self, finishAutoMove);

                        list.Add(finishAutoMoveAction);
                        break;
                    default:
                        throw new InvalidOperationException("Scheduled action not found");
                };
            }

            return list;
        }
    }
}
