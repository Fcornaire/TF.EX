using FortRise;
using TF.Replay.Domain;

namespace TF.Replay.Core
{
    public class ReplaySettings : ModuleSettings
    {
        private static readonly string[] SaveStateModes = { "FULL", "KEY" };

        private static readonly string[] GifQualities = { "VANILLA", "HIGH" };

        public bool RecordLastManStanding { get; set; } = true;

        public bool RecordHeadHunters { get; set; } = true;

        public bool RecordTeamDeathmatch { get; set; } = true;

        public bool RecordTrials { get; set; } = true;

        public string SaveState { get; set; } = "FULL";

        public string GifQuality { get; set; } = "HIGH";

        public override void Create(ISettingsCreate settings)
        {
            settings.CreateOnOff("RECORD LAST MAN STANDING", RecordLastManStanding, value =>
            {
                RecordLastManStanding = value;
                Apply();
            }, "AUTOMATICALLY RECORD LAST MAN STANDING MATCHES AS REPLAYS");

            settings.CreateOnOff("RECORD HEADHUNTERS", RecordHeadHunters, value =>
            {
                RecordHeadHunters = value;
                Apply();
            }, "AUTOMATICALLY RECORD HEADHUNTERS MATCHES AS REPLAYS");

            settings.CreateOnOff("RECORD TEAM DEATHMATCH", RecordTeamDeathmatch, value =>
            {
                RecordTeamDeathmatch = value;
                Apply();
            }, "AUTOMATICALLY RECORD TEAM DEATHMATCH MATCHES AS REPLAYS");

            settings.CreateOnOff("RECORD TRIALS", RecordTrials, value =>
            {
                RecordTrials = value;
                Apply();
            }, "AUTOMATICALLY RECORD TRIALS RUNS AS REPLAYS");

            settings.CreateOptions(
                "SAVE STATE",
                SaveState,
                SaveStateModes,
                selection =>
                {
                    SaveState = selection.Item1;
                    Apply();
                },
                "HOW A REPLAY SAVES THE GAME STATE. \n FULL SAVES IT EVERY FRAME, \n BUT A MATCH REPLAY TAKES MORE DISK SPACE. \n KEY SAVES A FEW STATES PER SECOND: MUCH SMALLER FILES, \n  BUT GOING BACK OR SEEKING SNAPS TO THE LAST SAVED STATE");

            settings.CreateOptions(
                "GIF QUALITY",
                GifQuality,
                GifQualities,
                selection =>
                {
                    GifQuality = selection.Item1;
                    Apply();
                },
                "QUALITY OF EXPORTED GIFS. \n VANILLA: 15 FPS, SAME AS THE GAME'S REPLAY GIFS. \n HIGH: 25 FPS AND BETTER COLORS, \n  LARGER FILES AND SLOWER EXPORT");
        }

        public override void OnVerify() => Apply();

        internal void Apply()
        {
            RecordingPolicy.RecordLastManStanding = RecordLastManStanding;
            RecordingPolicy.RecordHeadHunters = RecordHeadHunters;
            RecordingPolicy.RecordTeamDeathmatch = RecordTeamDeathmatch;
            RecordingPolicy.RecordTrials = RecordTrials;
            RecordingPolicy.FullStates = !string.Equals(SaveState, "KEY", StringComparison.OrdinalIgnoreCase);
            GifExport.Quality = string.Equals(GifQuality, "VANILLA", StringComparison.OrdinalIgnoreCase) ? Domain.GifQuality.Vanilla : Domain.GifQuality.High;
        }
    }
}
