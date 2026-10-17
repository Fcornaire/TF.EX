namespace TF.State.Domain.Context
{
    public static class SpawnHatRules
    {
        private static bool?[] _startsWithoutHat;

        public static void Set(bool?[] startsWithoutHat)
        {
            _startsWithoutHat = startsWithoutHat;
        }

        public static bool? Get(int playerIndex)
        {
            var rules = _startsWithoutHat;

            return rules != null && playerIndex >= 0 && playerIndex < rules.Length ? rules[playerIndex] : null;
        }
    }
}
