using MessagePack;
using System.Runtime.InteropServices;

namespace TF.EX.Domain.Models.State
{
    [StructLayout(LayoutKind.Sequential)]
    [MessagePackObject]

    public struct Vector2f
    {
        [Key(0)]
        public float X { get; set; }

        [Key(1)]
        public float Y { get; set; }


        public override string ToString() => $"({X}, {Y})";

        public bool IsAfterThreshold() => Math.Abs(X) > 0.5f || Math.Abs(Y) > 0.5f;

        public static bool operator >(Vector2f a, Vector2f b) => Math.Abs(a.X) > Math.Abs(b.X) && Math.Abs(a.Y) > Math.Abs(b.Y);

        public static bool operator <(Vector2f a, Vector2f b) => Math.Abs(a.X) < Math.Abs(b.X) && Math.Abs(a.Y) < Math.Abs(b.Y);

    }
}
