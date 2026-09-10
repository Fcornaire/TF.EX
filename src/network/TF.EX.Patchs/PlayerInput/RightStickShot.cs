using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Patchs.PlayerInput
{
    public static class RightStickShot
    {
        private const float ArmThresholdSquare = 0.25f;
        private const float NeutralThresholdSquare = 0.09f;
        private const string HeldKey = "rightStickHeld";

        public static InputState Apply(InputState state, Vector2 rightStick, Level level, int seat)
        {
            var player = level.GetPlayer(seat);

            if (player == null || !IsVariantActive(level))
            {
                return state;
            }

            var dynPlayer = DynamicData.For(player);
            var wasHeld = dynPlayer.Get("isAimingRight") as bool? ?? false;
            var held = rightStick.LengthSquared() >= (wasHeld ? NeutralThresholdSquare : ArmThresholdSquare);
            dynPlayer.Set(HeldKey, held);

            if (held)
            {
                state.ShootCheck = true;
                state.AimAxis = rightStick;

                if (!wasHeld)
                {
                    state.ShootPressed = true;
                }
            }
            else if (wasHeld && player.Aiming)
            {
                var lastAimDirection = dynPlayer.Get<float>("lastAimDirection");

                if (lastAimDirection != -1f)
                {
                    state.AimAxis = Calc.AngleToVector(lastAimDirection, 1f);
                }
            }

            return state;
        }

        public static void UpdateIsAimingRight(Player player)
        {
            var dynPlayer = DynamicData.For(player);
            dynPlayer.Set("isAimingRight", dynPlayer.Get(HeldKey) as bool? ?? false);
        }

        private static bool IsVariantActive(Level level)
        {
            var variant = ServiceCollections.ResolveContext().Registry.Variants.GetVariant(Constants.RIGHT_STICK_VARIANT_NAME);

            return variant != null
                && level.Session.MatchSettings.Variants.CustomVariants.TryGetValue(variant.Name, out var value)
                && value.Value;
        }
    }
}
