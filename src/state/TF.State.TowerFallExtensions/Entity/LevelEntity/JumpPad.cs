using MonoMod.Utils;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class JumpPadExtensions
    {
        public static JumpPad GetState(this TowerFall.JumpPad jumpPad)
        {
            var dynJumpPad = DynamicData.For(jumpPad);
            var images = dynJumpPad.Get<Monocle.Image[]>("images");

            return new JumpPad
            {
                ActualDepth = dynJumpPad.Get<double>("actualDepth"),
                IsOn = dynJumpPad.Get<bool>("on"),
                ImageScaleYs = images.Select(image => image.Scale.Y).ToArray(),
            };
        }

        public static void LoadState(this TowerFall.JumpPad jumpPad, JumpPad state)
        {
            var dynJumpPad = DynamicData.For(jumpPad);

            dynJumpPad.Set("on", state.IsOn);

            if (state.ImageScaleYs != null)
            {
                var images = dynJumpPad.Get<Monocle.Image[]>("images");
                for (int i = 0; i < images.Length && i < state.ImageScaleYs.Length; i++)
                {
                    images[i].Scale.Y = state.ImageScaleYs[i];
                }
            }
        }

    }
}
