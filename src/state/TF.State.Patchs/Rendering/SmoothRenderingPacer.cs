using HarmonyLib;
using Microsoft.Xna.Framework;
using TF.State.Domain.Context;
using TowerFall;

namespace TF.State.Patchs.Rendering
{
    [HarmonyPatch(typeof(TowerFall.TFGame))]
    internal static class SmoothRenderingPacerPatch
    {
        private const double Step = 1.0 / 60.0;
        private const double MaxBacklog = Step * 5;

        private static double _accumulator;
        private static bool _forcedFixedTimeStep;

        private static readonly Action<GameTime, TimeSpan> SetElapsedGameTime = BuildElapsedSetter();

        private static Action<GameTime, TimeSpan> BuildElapsedSetter()
        {
            var setter = AccessTools.PropertySetter(typeof(GameTime), nameof(GameTime.ElapsedGameTime));

            return setter == null ? null : (Action<GameTime, TimeSpan>)setter.CreateDelegate(typeof(Action<GameTime, TimeSpan>));
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch("Update")]
        public static bool TFGame_Update_Prefix(TowerFall.TFGame __instance, GameTime gameTime)
        {
            if (!StateFlags.SmoothRendering || __instance.Scene is not Level)
            {
                if (_accumulator != 0)
                {
                    _accumulator = 0;
                    RenderInterpolation.Invalidate();
                }

                StateFlags.InterpolationAlpha = 1f;

                return true;
            }

            _accumulator += gameTime.ElapsedGameTime.TotalSeconds;

            if (_accumulator > MaxBacklog)
            {
                _accumulator = MaxBacklog;
            }

            if (_accumulator < Step)
            {
                StateFlags.InterpolationAlpha = (float)(_accumulator / Step);

                return false;
            }

            _accumulator -= Step;
            StateFlags.InterpolationAlpha = (float)(_accumulator / Step);

            RenderInterpolation.BeginStep();

            SetElapsedGameTime?.Invoke(gameTime, __instance.TargetElapsedTime);

            __instance.IsFixedTimeStep = true;
            _forcedFixedTimeStep = true;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void TFGame_Update_Postfix(TowerFall.TFGame __instance)
        {
            if (_forcedFixedTimeStep)
            {
                __instance.IsFixedTimeStep = false;
                _forcedFixedTimeStep = false;
            }
        }
    }
}
