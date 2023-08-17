using TF.EX.Domain;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Patchs.Scene
{
    internal class MapScenePatch : IHookable
    {
        private readonly IInputService _inputService;

        public MapScenePatch(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Load()
        {
            On.TowerFall.MapScene.StartSession += MapScene_StartSession;
        }

        public void Unload()
        {
            On.TowerFall.MapScene.StartSession -= MapScene_StartSession;
        }

        private void MapScene_StartSession(On.TowerFall.MapScene.orig_StartSession orig, TowerFall.MapScene self)
        {
            orig(self);

            _inputService.EnsureRemoteController();
            ServiceCollections.PurgeCache();
        }
    }
}
