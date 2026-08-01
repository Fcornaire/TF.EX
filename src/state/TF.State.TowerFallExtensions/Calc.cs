using Microsoft.Xna.Framework;
using TowerFall;

namespace TF.State.TowerFallExtensions
{
    public static class CalcExtensions
    {

        public static IEnumerable<Vector2> OwnVectorShuffle(IEnumerable<Vector2> elem)
        {
            return OwnShuffle(elem);
        }

        public static IEnumerable<MapButton> OwnMapButtonShuffle(IEnumerable<MapButton> elem)
        {
            return OwnShuffle(elem);
        }

        public static IEnumerable<int> OwnShuffledIndexes(int count)
        {
            return OwnShuffle(Enumerable.Range(0, count));
        }

        private static IEnumerable<T> OwnShuffle<T>(IEnumerable<T> elem)
        {
            var toShuffle = elem.ToList();
            Monocle.Calc.Shuffle(toShuffle, Monocle.Calc.Random);
            return toShuffle;
        }
    }
}
