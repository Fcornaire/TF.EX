namespace TF.EX.Domain.Interop
{
    public interface ITfStateApi
    {
        int ApiVersion { get; }
        string GetStateSchemaVersion();

        bool IsStateAvailable();
        byte[] GetGameStateBytes();
        byte[] GetGameStateBytesForRecording();
        bool LoadGameStateBytes(byte[] state);
        int GetFrameOf(byte[] state);
        string CompareStates(byte[] stateA, byte[] stateB);
        string[] DescribePlayers(byte[] state);
        string[] ClassifyStateDiff(byte[] liveState, byte[] recordedState);
        bool StateMatchesWithFrame(byte[] candidate, byte[] liveState, int frame);
        void ResetRngOverride();
        void StepOwnedControllers();

        int RestoreGameStateBytes(byte[] state);
        byte[] CaptureGameState();

        void SetFrameDriver(string ownerModName);
        string GetFrameDriver();

        bool IsSmoothRendering();

        void SetDriverFlags(int currentFrame, bool isCaptureActive, bool isTestMode,
                            bool isReplayMode, bool isRollbackFrame, double framesToReSimulate);
        void SetRestoring(bool value);
        void SetCurrentFrame(int frame);
        int GetCurrentFrame();

        void SynchronizeSfx(int currentFrame, bool isTestMode);
        void ClearSfx();
        void ResetSfx();
        void ResetSession();
        void SetSessionRoundStarted(bool value);

        string DumpEntities();
        void GenerateVersusLevels(TowerFall.MatchSettings matchSettings, int mapId, int startLevel);

        void SetSeed(int seed);
        int GetSeed();
        void ResetRng();

        void ResetMatch();
        void PurgeCache();
    }
}
