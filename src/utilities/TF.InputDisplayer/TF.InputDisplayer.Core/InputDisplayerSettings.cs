using FortRise;
using TF.InputDisplayer.Domain;

namespace TF.InputDisplayer.Core
{
    public class InputDisplayerSettings : ModuleSettings
    {
        private const int MinRows = 4;
        private const int MinOpacity = 3;
        private const int MaxOpacity = 10;
        private const int MaxRows = InputDisplay.MaxRowsPerColumn;

        public bool Enabled { get; set; } = true;

        public bool ShowInInstantReplay { get; set; } = true;

        public int MaxHistoryRows { get; set; } = MaxRows;

        public int Opacity { get; set; } = MaxOpacity;

        public override void Create(ISettingsCreate settings)
        {
            settings.CreateOnOff("INPUT DISPLAY", Enabled, value =>
            {
                Enabled = value;
                Apply();
            });

            settings.CreateOnOff("SHOW IN INSTANT REPLAY", ShowInInstantReplay, value =>
            {
                ShowInInstantReplay = value;
                Apply();
            });

            settings.CreateNumber("HISTORY ROWS", MaxHistoryRows, value =>
            {
                MaxHistoryRows = value;
                Apply();
            }, MinRows, MaxRows, 1);

            settings.CreateNumber("OPACITY", Opacity, value =>
            {
                Opacity = value;
                Apply();
            }, MinOpacity, MaxOpacity, 1);
        }

        public override void OnVerify() => Apply();

        internal void Apply()
        {
            DisplayOptions.Enabled = Enabled;
            DisplayOptions.ShowInInstantReplay = ShowInInstantReplay;
            DisplayOptions.MaxRows = Math.Clamp(MaxHistoryRows, MinRows, MaxRows);
            DisplayOptions.Opacity = Math.Clamp(Opacity, MinOpacity, MaxOpacity) / 10f;
        }
    }
}
