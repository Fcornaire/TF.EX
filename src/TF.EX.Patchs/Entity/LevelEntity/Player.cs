using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class PlayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public PlayerPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.Player.DoWrapRender += Player_DoWrapRender;
        }

        public void Unload()
        {
            On.TowerFall.Player.DoWrapRender -= Player_DoWrapRender;
        }

        private void Player_DoWrapRender(On.TowerFall.Player.orig_DoWrapRender orig, TowerFall.Player self)
        {
            if (_netplayManager.IsTestMode() || _netplayManager.IsReplayMode())
            {
                self.DebugRender();
            }
            orig(self);
        }

    }
}
