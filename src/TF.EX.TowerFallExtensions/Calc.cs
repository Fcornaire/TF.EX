using Microsoft.Xna.Framework;

namespace TF.EX.TowerFallExtensions
{
    public static class CalcExtensions
    {

        public static List<Vector2> OwnVectorShuffle(Vector2[] elem)
        {
            var toShuffle = elem.ToList();
            Monocle.Calc.Shuffle(toShuffle, Monocle.Calc.Random);
            return toShuffle;
        }
    }
}
