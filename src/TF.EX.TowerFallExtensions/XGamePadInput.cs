using Microsoft.Xna.Framework;
using TF.EX.Domain.Models.State;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class XGamePadInputExtensions
    {
        public static RightStick GetRightStick(this XGamepadInput input)
        {
            var xGamepad = input.XGamepad;
            var vector = xGamepad.GetRightStick();

            return new RightStick
            {
                MoveX = (!(Math.Abs(vector.X) < 0.5f)) ? Math.Sign(vector.X) : 0,
                MoveY = (!(Math.Abs(vector.Y) < 0.8f)) ? Math.Sign(vector.Y) : 0,
                AimAxis = (vector.LengthSquared() < 0.09f) ? Vector2.Zero : vector
            };
        }
    }
}
