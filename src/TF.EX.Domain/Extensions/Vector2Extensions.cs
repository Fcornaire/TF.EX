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

        public static Vector2 GetPositionByPlayerDraw(this List<Vector2> positions, bool shouldSwapPlayer, int originalIndex)
        {

            if (!shouldSwapPlayer)
            {
                return positions[originalIndex];
            }

            switch (originalIndex)
            {
                case 0:
                    return positions[1];
                case 1:
                    return positions[0];
                default: return positions[originalIndex]; //TODO: whats about more than 2P ?
            }

        }
    }
}
