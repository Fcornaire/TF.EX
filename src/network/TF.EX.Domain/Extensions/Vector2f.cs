using Microsoft.Xna.Framework;
using TF.EX.Domain.Models;

namespace TF.EX.Domain.Extensions
{
    public static class Vector2fExtensions
    {
        public static Vector2 ToTFVector(this Vector2f vector2) => new Vector2(vector2.X, vector2.Y);

        public static Vector2f ToModel(this Vector2 vector2) => new Vector2f { X = vector2.X, Y = vector2.Y };
    }
}
