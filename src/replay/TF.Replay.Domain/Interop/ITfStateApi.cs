namespace TF.Replay.Domain.Interop
{
    public interface ITfStateApi
    {
        int ApiVersion { get; }
        string GetStateSchemaVersion();

        bool IsStateAvailable();
        bool IsTestMode();
        byte[] GetGameStateBytes();
        byte[] GetGameStateBytesForRecording();
        bool LoadGameStateBytes(byte[] state);

        bool IsRoundStarted(byte[] state);

        bool IsRoundResultsShown(byte[] state);

        string[] ClassifyStateDiff(byte[] liveState, byte[] recordedState);

        string[] DescribePlayers(byte[] state);

        bool HasStateEvents(string modName);

        void SetFrameDriver(string ownerModName);
        string GetFrameDriver();

        void SetDriverFlags(int currentFrame, bool isCaptureActive, bool isTestMode,
                            bool isReplayMode, bool isRollbackFrame, double framesToReSimulate);
        void SetCurrentFrame(int frame);
        int GetCurrentFrame();

        void SynchronizeSfx(int currentFrame, bool isTestMode);

        void SetSeed(int seed);
        int GetSeed();

        void StepOwnedControllers();

        long GetTrialsTime();
        bool IsTrialsOver();
    }

    public static class TfStateApiData
    {
        public const string Name = "TF.State";
    }
}
