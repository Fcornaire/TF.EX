using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models.State
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2f
    {
        public float x;
        public float y;

        public Vector2f(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
