using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2f ToModel(this Vector2 vector2)
        {
            return new Vector2f
            {
                X = vector2.X,
                Y = vector2.Y
            };
        }

        public static Vector2f[] ToModel(this Vector2[] vectors2)
        {
            return vectors2.Select(vec => vec.ToModel()).ToArray();
        }

        public static Vector2[] ToTFVector(this Vector2f[] vectors2)
        {
            return vectors2.Select(vec => vec.ToTFVector()).ToArray();
        }

        public static Vector2 ToTFVector(this Vector2f vector2)
        {
            return new Vector2(vector2.X, vector2.Y);
        }

    }
}
