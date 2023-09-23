using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models.State
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2f
    {
        public float X { get; set; }
        public float Y { get; set; }


        public override string ToString() => $"({X}, {Y})";
    }
}
