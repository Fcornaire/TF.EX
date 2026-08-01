using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace TF.State.Domain.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RightStick
    {
        public int MoveX;

        public int MoveY;

        public Vector2 AimAxis;
    }
}
