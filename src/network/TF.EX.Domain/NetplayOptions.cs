using TowerFall;

namespace TF.EX.Domain
{
    public static class NetplayOptions
    {
        private static bool _applied;

        private static bool _canSkipReplays;
        private static bool _showTips;
        private static Options.ReplayModes _replayMode;

        public static bool IsApplied => _applied;

        public static void Apply()
        {
            var options = SaveData.Instance?.Options;

            if (options == null || _applied)
            {
                return;
            }

            _canSkipReplays = options.CanSkipReplays;
            _showTips = options.ShowTips;
            _replayMode = options.ReplayMode;
            _applied = true;

            Force(options);
        }

        public static void Restore()
        {
            var options = SaveData.Instance?.Options;

            if (options == null || !_applied)
            {
                _applied = false;
                return;
            }

            _applied = false;
            Own(options);
        }

        public static void BeforeSave()
        {
            if (_applied)
            {
                Own(SaveData.Instance?.Options);
            }
        }

        public static void AfterSave()
        {
            if (_applied)
            {
                Force(SaveData.Instance?.Options);
            }
        }

        private static void Force(Options options)
        {
            if (options == null)
            {
                return;
            }

            options.CanSkipReplays = true;
            options.ShowTips = false;
            options.ReplayMode = Options.ReplayModes.UseGPU;
        }

        private static void Own(Options options)
        {
            if (options == null)
            {
                return;
            }

            options.CanSkipReplays = _canSkipReplays;
            options.ShowTips = _showTips;
            options.ReplayMode = _replayMode;
        }
    }
}
