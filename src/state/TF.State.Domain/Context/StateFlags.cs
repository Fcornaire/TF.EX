namespace TF.State.Domain.Context
{
    public static class StateFlags
    {
        public static int CurrentFrame;

        public static bool IsCaptureActive;

        public static bool IsSfxCaptureActive;

        public static bool IsTestMode;

        public static bool IsReplayMode;

        public static bool IsRestoring;

        public static bool IsRollbackFrame;

        public static double FramesToReSimulate;

        public static bool HasFramesToReSimulate => FramesToReSimulate > 0;

        public static string FrameDriverOwner;

        public static bool SmoothRendering;

        public static float InterpolationAlpha = 1f;

        public static void Reset()
        {
            CurrentFrame = 0;
            IsCaptureActive = false;
            IsSfxCaptureActive = false;
            IsTestMode = false;
            IsReplayMode = false;
            IsRestoring = false;
            IsRollbackFrame = false;
            FramesToReSimulate = 0;
        }
    }
}
