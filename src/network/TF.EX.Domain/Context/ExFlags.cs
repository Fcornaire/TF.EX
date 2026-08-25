using TF.EX.Domain.Interop;

namespace TF.EX.Domain.Context
{
    public static class ExFlags
    {
        public static int CurrentFrame;

        public static bool IsCaptureActive;

        public static bool IsTestMode;

        public static bool IsReplayMode;

        public static bool IsRestoring;

        public static bool IsRollbackFrame;

        public static double FramesToReSimulate;

        public static bool HasFramesToReSimulate => FramesToReSimulate > 0;

        public static void Push()
        {
            var owner = StateApi.Current.GetFrameDriver();

            if (!string.IsNullOrEmpty(owner) && owner != Models.Constants.DRIVER_NAME)
            {
                return;
            }

            StateApi.Current.SetDriverFlags(CurrentFrame, IsCaptureActive, IsTestMode, IsReplayMode, IsRollbackFrame, FramesToReSimulate);
            StateApi.Current.SetSfxCapture(IsCaptureActive && !IsReplayMode);
        }

        public static void Reset()
        {
            CurrentFrame = 0;
            IsCaptureActive = false;
            IsTestMode = false;
            IsReplayMode = false;
            IsRestoring = false;
            IsRollbackFrame = false;
            FramesToReSimulate = 0;
        }
    }
}
