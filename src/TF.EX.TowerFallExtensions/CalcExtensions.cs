using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace TF.EX.TowerFallExtensions
{
    public static class CalcExtensions
    {

        public static List<Vector2> OwnVectorShuffle(Vector2[] elem)
        {
            var toShuffle = elem.ToList();
            Calc.Shuffle(toShuffle, Calc.Random);
            return toShuffle;
        }
    }
}
