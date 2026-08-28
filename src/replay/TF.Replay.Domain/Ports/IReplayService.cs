using TF.Replay.Domain.Models;

namespace TF.Replay.Domain.Ports
{
    public interface IReplayService
    {
        bool IsRecording { get; }
        bool IsPlayback { get; }
        int PlaybackFrame { get; }
        int RecordCount { get; }

        int LastFrame { get; }

        void BeginRecording(int towerId, int mode, int versusMatchLength, IEnumerable<string> variants,IEnumerable<CustomMod> mods);
        void AddRecord(byte[] state, int[] inputs, int frame = -1);
        void RemovePredictedRecords(int frame);
        string Export();
        void Reset();
        void RestorePendingTimeStep();

        void SetLocalSeat(int seat);
        void SetPlayerCount(int count);
        void SetSeed(int seed);

        void SetTrialsLevel(int x, int y);

        void SetTrialsTime(long ticks);
        void SetArchers(IEnumerable<ArcherInfo> archers);
        void SetTeams(int[] teams);

        Task<PlaybackStartResult> LoadAndStart(string replayFilename);

        bool NeedsMigration(ReplayInfo informations);

        string MissingMod(ReplayInfo informations);

        string SeekBlockedBy { get; }

        void StartSession();

        void ApplyRecordedVariants();

        void StopPlayback();
        void EnsurePlaybackTickRate();
        Record GetRecordAt(int frame);
        Record ConsumeNextRecord();

        bool SeekTo(int frame);

        int SeekLandingFor(int frame);

        bool RestoreStateAt(int frame);

        string CurrentMonth { get; }

        string CurrentFolder { get; }

        string[] GetMonths();
        void SetMonth(string month);

        Task<IEnumerable<Models.Replay>> LoadAndGetReplays();
        Models.Replay GetReplay();
        void LoadReplay(Models.Replay replay);
    }

    public sealed class PlaybackStartResult
    {
        public bool Started { get; init; }
        public string FailureReason { get; init; }
        public int LocalSeat { get; init; }

        public static PlaybackStartResult Ok(int localSeat) => new() { Started = true, LocalSeat = localSeat };
        public static PlaybackStartResult Fail(string reason) => new() { Started = false, FailureReason = reason };
    }
}
