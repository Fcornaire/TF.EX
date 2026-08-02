using FortRise;
using TF.State.Domain.Context;

namespace TF.State.Core
{
    public class StateSettings : ModuleSettings
    {
        public bool SmoothRendering { get; set; } = true;

        public override void Create(ISettingsCreate settings)
        {
            settings.CreateOnOff("SMOOTH RENDERING", SmoothRendering, value =>
            {
                SmoothRendering = value;
                Apply();
            });
        }

        public override void OnVerify() => Apply();

        internal void Apply()
        {
            StateFlags.SmoothRendering = SmoothRendering;

            if (!SmoothRendering)
            {
                StateFlags.InterpolationAlpha = 1f;
                RenderInterpolation.Invalidate();
            }
        }
    }
}
