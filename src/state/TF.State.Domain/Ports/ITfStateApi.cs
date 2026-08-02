using System;

namespace TF.State.Domain.Ports
{
    public interface ITfStateApi
    {
        int ApiVersion { get; }

        string GetStateSchemaVersion();

        bool IsStateAvailable();

        byte[] GetGameStateBytes();

        byte[] GetGameStateBytesForRecording();

        bool LoadGameStateBytes(byte[] state);

        int RestoreGameStateBytes(byte[] state);

        byte[][] CaptureGameStateAndRecording();

        int GetFrameOf(byte[] state);

        void SetFrameDriver(string ownerModName);
        string GetFrameDriver();

        bool IsSmoothRendering();

        void SetDriverFlags(int currentFrame, bool isCaptureActive, bool isTestMode,bool isReplayMode, bool isRollbackFrame, double framesToReSimulate);

        void SetRestoring(bool value);
        void SetCurrentFrame(int frame);
        int GetCurrentFrame();

        void SynchronizeSfx(int currentFrame, bool isTestMode);
        void ClearSfx();
        void ResetSfx();
        void ResetSession();
        void SetSessionRoundStarted(bool value);

        string CompareStates(byte[] stateA, byte[] stateB);

        string[] DescribePlayers(byte[] state);

        string DescribeState(byte[] state, int maxDepth);

        string[] ClassifyStateDiff(byte[] liveState, byte[] recordedState);

        bool StateMatchesWithFrame(byte[] candidate, byte[] liveState, int frame);

        void ResetRngOverride();

        void StepOwnedControllers();

        long GetTrialsTime();

        bool IsTrialsOver();

        string DumpEntities();

        void GenerateVersusLevels(TowerFall.MatchSettings matchSettings, int mapId, int startLevel);

        void SetSeed(int seed);
        int GetSeed();
        void ResetRng();
        void RegisterRng();
        void UnregisterRng();

        void ResetRound();
        void ResetMatch();
        void PurgeCache();

        void RegisterStateEvents(string modName, string key, Func<byte[]> onSaveState, Action<byte[]> onLoadState);
        void UnregisterStateEvents(string modName, string key);
        void MarkModuleAsSafe(string modName);
        bool IsModuleSafe(string modName);
        bool HasStateEvents(string modName);
    }

    public static class TfStateApiData
    {
        public const string Name = "TF.State";
    }
}
