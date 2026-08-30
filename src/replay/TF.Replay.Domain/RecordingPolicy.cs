using TowerFall;

namespace TF.Replay.Domain
{
    public static class RecordingPolicy
    {
        public static bool RecordLastManStanding { get; set; } = true;
        public static bool RecordHeadHunters { get; set; } = true;
        public static bool RecordTeamDeathmatch { get; set; } = true;
        public static bool RecordTrials { get; set; } = true;
        public static bool FullStates { get; set; } = true;

        public static bool Allows(int mode) => (Modes)mode switch
        {
            Modes.LastManStanding => RecordLastManStanding,
            Modes.HeadHunters => RecordHeadHunters,
            Modes.TeamDeathmatch => RecordTeamDeathmatch,
            Modes.Trials => RecordTrials,
            _ => true,
        };
    }
}
