using System;

namespace TF.Replay.Domain.Ports
{
    public interface ITfReplayApi
    {
        int ApiVersion { get; }
        string GetReplayFormatVersion();

        int InputStride { get; }

        void SetRecordDriver(string ownerModName);
        string GetRecordDriver();

        void AddRecordingMod(string name, string[] dataKeys, string[] dataValues);

        void BeginRecording(int towerId, int mode, int versusMatchLength, string[] variants);
        void AddRecord(byte[] state, int[] inputs, int frame);
        void RemovePredictedRecords(int frame);
        void AddDesyncRecord(byte[] state, int[] inputs, int frame);
        string Export();
        void ResetRecording();

        void SetLocalSeat(int seat);
        void SetPlayerCount(int count);
        void SetSeed(int seed);
        void SetArcher(int seat, int archerIndex, int archerType, bool hasWon, int score, string name);

        void SetArchersFlat(int[] indexes, int[] types, bool[] hasWon, int[] scores, string[] names);

        void SetArcherTeams(int[] teams);

        bool IsRecording { get; }
        bool IsPlayback { get; }
        int PlaybackFrame { get; }
        int RecordCount { get; }

        int ConsumeNextRecordFrame();

        int GetLoadedReplayMode();

        int[] GetInputsAtFrame(int frame);
        byte[] GetStateAtFrame(int frame);

        void RenderInputOverlay(int[] flatInputs);

        string StartPlayback(string replayFileName);
        void StopPlayback();
        bool SeekTo(int frame);

        bool UpdatePlaybackControls();

        void SetPlaybackStartedCallback(Action<int> onStarted);
        void SetPlaybackStoppedCallback(Action onStopped);

        void SetHostCallbacks(Action<bool> setInputEnabled, Action ensureFakeControllers,Action<string> notify, Func<string, string, string> launchReplay);
    }

    public static class TfReplayApiData
    {
        public const string Name = "TF.Replay";
    }
}
