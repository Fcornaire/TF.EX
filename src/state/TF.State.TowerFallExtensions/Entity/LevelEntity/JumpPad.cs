using MonoMod.Utils;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class JumpPadExtensions
    {
        public static JumpPad GetState(this TowerFall.JumpPad jumpPad)
        {
            var dynJumpPad = DynamicData.For(jumpPad);

            return new JumpPad
            {
                ActualDepth = dynJumpPad.Get<double>("actualDepth"),
                IsOn = dynJumpPad.Get<bool>("on"),
            };
        }

        public static void LoadState(this TowerFall.JumpPad jumpPad, JumpPad state)
        {
            var dynJumpPad = DynamicData.For(jumpPad);

            dynJumpPad.Set("on", state.IsOn);
        }

    }
}
