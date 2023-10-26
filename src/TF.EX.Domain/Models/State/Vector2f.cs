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
    }
}
