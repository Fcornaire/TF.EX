using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models.State
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RightStick
    {
        public int MoveX;

        public int MoveY;

        public Vector2 AimAxis;
    }
}
