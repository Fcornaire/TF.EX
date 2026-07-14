using System;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class BrambleSpreadController
    {
        private static readonly Vector2[] Checks = new Vector2[5]
        {
            Vector2.Zero,
            Vector2.UnitX * 4f,
            Vector2.UnitX * -4f,
            Vector2.UnitY * 4f,
            Vector2.UnitY * -4f,
        };

        public static BrambleSpreadState Start(int id, Vector2 at, int ownerIndex, int spread = 3, bool shortTime = false)
        {
            var state = new BrambleSpreadState
            {
                Id = id,
                At = at.ToModel(),
                OwnerIndex = ownerIndex,
                Spread = spread,
                ShortTime = shortTime,
                Waited = 0,
                SoundPlayed = false,
                TimeWaited = 0f,
                IsComplete = false,
            };
            state.ToCheck.Add(at.ToModel());
            state.Spreads.Add(spread);
            return state;
        }

        public static void Step(TowerFall.BrambleArrow arrow, BrambleSpreadState state)
        {
            if (state == null || state.IsComplete)
            {
                return;
            }

            var level = arrow.Level;

            if (state.Made.Count > 0 && state.TimeWaited >= 0.5f && !state.SoundPlayed)
            {
                state.SoundPlayed = true;
                Sounds.pu_brambleGrow.Play(state.At.X);
            }

            while (state.ToCheck.Count > 0)
            {
                var vector = state.ToCheck[0].ToTFVector();
                state.ToCheck.RemoveAt(0);
                var num = state.Spreads[0];
                state.Spreads.RemoveAt(0);

                if (!Contains(state, vector))
                {
                    var bramble = TowerFall.Brambles.Create(state.Id, vector, state.OwnerIndex, 8 + (state.Spread - num) * 2 - state.Waited, state.ShortTime);
                    bramble.Depth += num;
                    level.Add(bramble);
                    state.Made.Add(vector.ToModel());

                    if (num > 0)
                    {
                        TrySpread(level, state, vector - Vector2.UnitX * 8f, num);
                        TrySpread(level, state, vector + Vector2.UnitX * 8f, num);
                        TrySpread(level, state, vector - Vector2.UnitY * 8f, num);
                        TrySpread(level, state, vector + Vector2.UnitY * 8f, num);
                    }

                    state.Waited++;
                    state.TimeWaited += 0.6f;
                    return; 
                }
            }

            state.IsComplete = true;
            SetBrothers(level, state.Id);
            DynamicData.For(arrow).Set("canDie", true); 
        }

        private static void TrySpread(Level level, BrambleSpreadState state, Vector2 to, int spread)
        {
            foreach (var check in Checks)
            {
                if (TowerFall.Brambles.CanSpreadTo(level, to + check))
                {
                    state.ToCheck.Add((to + check).ToModel());
                    state.Spreads.Add(spread - 1);
                    break;
                }
            }
        }

        private static bool Contains(BrambleSpreadState state, Vector2 vector)
        {
            foreach (var made in state.Made)
            {
                if (made.X == vector.X && made.Y == vector.Y)
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetBrothers(Level level, int id)
        {
            var brothers = level.GetAll<TowerFall.Brambles>().Where(b => b.ID == id).ToList();
            foreach (var bramble in brothers)
            {
                DynamicData.For(bramble).Invoke("SetBrothers", brothers);
            }
        }
    }
}
