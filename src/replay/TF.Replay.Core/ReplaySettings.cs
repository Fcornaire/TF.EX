using FortRise;
using TF.Replay.Domain;

namespace TF.Replay.Core
{
    public class ReplaySettings : ModuleSettings
    {
        public bool RecordLastManStanding { get; set; } = true;

        public bool RecordHeadHunters { get; set; } = true;

        public bool RecordTeamDeathmatch { get; set; } = true;

        public bool RecordTrials { get; set; } = true;

        public override void Create(ISettingsCreate settings)
        {
            settings.CreateOnOff("RECORD LAST MAN STANDING", RecordLastManStanding, value =>
            {
                RecordLastManStanding = value;
                Apply();
            }, "Automatically record Last Man Standing matches as replays");

            settings.CreateOnOff("RECORD HEADHUNTERS", RecordHeadHunters, value =>
            {
                RecordHeadHunters = value;
                Apply();
            }, "Automatically record Headhunters matches as replays");

            settings.CreateOnOff("RECORD TEAM DEATHMATCH", RecordTeamDeathmatch, value =>
            {
                RecordTeamDeathmatch = value;
                Apply();
            }, "Automatically record Team Deathmatch matches as replays");

            settings.CreateOnOff("RECORD TRIALS", RecordTrials, value =>
            {
                RecordTrials = value;
                Apply();
            }, "Automatically record Trials runs as replays");
        }

        public override void OnVerify() => Apply();

        internal void Apply()
        {
            RecordingPolicy.RecordLastManStanding = RecordLastManStanding;
            RecordingPolicy.RecordHeadHunters = RecordHeadHunters;
            RecordingPolicy.RecordTeamDeathmatch = RecordTeamDeathmatch;
            RecordingPolicy.RecordTrials = RecordTrials;
        }
    }
}
