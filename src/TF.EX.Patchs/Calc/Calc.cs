using TF.EX.Domain;

namespace TF.EX.Patchs.Calc
{
    public static class CalcPatch
    {
        private static int _gameplayDepth = 0;
        private static int _ignoreDepth = 0;
        private static bool _overriding = false;
        private static System.Random _cosmeticBackup = null;

        private static void Apply()
        {
            bool wantGameplay = _gameplayDepth > 0 && _ignoreDepth == 0;

            if (wantGameplay && !_overriding)
            {
                _cosmeticBackup = Monocle.Calc.Random;
                Monocle.Calc.Random = ServiceCollections.ResolveRngService().Gameplay;
                _overriding = true;
            }
            else if (!wantGameplay && _overriding)
            {
                Monocle.Calc.Random = _cosmeticBackup;
                _cosmeticBackup = null;
                _overriding = false;
            }
        }

        public static void RegisterRng()
        {
            _gameplayDepth++;
            Apply();
        }

        public static void UnregisterRng()
        {
            if (_gameplayDepth > 0)
            {
                _gameplayDepth--;
            }

            Apply();
        }
        public static void IgnoreToRegisterRng()
        {
            _ignoreDepth++;
            Apply();
        }

        public static void UnignoreToRegisterRng()
        {
            if (_ignoreDepth > 0)
            {
                _ignoreDepth--;
            }

            Apply();
        }

        public static void Reset()
        {
            _gameplayDepth = 0;
            _ignoreDepth = 0;

            if (_overriding)
            {
                Monocle.Calc.Random = _cosmeticBackup;
                _cosmeticBackup = null;
                _overriding = false;
            }
        }
    }
}
