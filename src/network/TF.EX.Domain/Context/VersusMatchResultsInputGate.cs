using TowerFall;

namespace TF.EX.Domain.Context
{
    // Neutral ggrs input once the match results panel has been on screen for a full prediction window without a rollback
    public static class VersusMatchResultsInputGate
    {
        private static int _shownFrames;
        private static bool _isRollbacking;

        public static void NotifyRollback() => _isRollbacking = true;

        public static void Reset()
        {
            _shownFrames = 0;
            _isRollbacking = false;
        }

        public static bool ShouldFeedNeutralInput(Level level) => !_isRollbacking && IsVersusMatchResultsShown(level) && _shownFrames > Models.Constants.MAX_PREDICTION_TICKS;

        public static void OnFrameAdvanced(Level level)
        {
            if (_isRollbacking || !IsVersusMatchResultsShown(level))
            {
                Reset();

                return;
            }

            _shownFrames++;
        }

        private static bool IsVersusMatchResultsShown(Level level)
        {
            return level != null && level.Layers.Values.Any(layer => layer.Entities.Any(entity => entity is VersusMatchResults));
        }
    }
}
