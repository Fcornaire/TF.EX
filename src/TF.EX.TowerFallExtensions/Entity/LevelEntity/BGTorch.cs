using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Background;

namespace TF.EX.TowerFallExtensions.Entity.LevelEntity
{
    public static class BGTorchExtensions
    {
        public static BGTorch GetState(this TowerFall.BGTorch bgTorch)
        {
            var dynTorch = DynamicData.For(bgTorch);
            var actualDepth = dynTorch.Get<double>("actualDepth");

            return new BGTorch
            {
                LightVisible = bgTorch.LightVisible,
                ActualDepth = actualDepth
            };
        }

        public static void LoadState(this TowerFall.BGTorch bgTorch, BGTorch toLoad)
        {
            bgTorch.LightVisible = toLoad.LightVisible;

            var dynTorch = DynamicData.For(bgTorch);
            dynTorch.Set("actualDepth", toLoad.ActualDepth);
        }
    }
}
