using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;

namespace TF.EX.Patchs
{
    internal class ReplayRecorderPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public ReplayRecorderPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.ReplayRecorder.ClearFrames += ReplayRecorder_ClearFrames;
        }

        public void Unload()
        {
            On.TowerFall.ReplayRecorder.ClearFrames -= ReplayRecorder_ClearFrames;
        }

        private void ReplayRecorder_ClearFrames(On.TowerFall.ReplayRecorder.orig_ClearFrames orig, TowerFall.ReplayRecorder self)
        {
            var mode = TowerFall.MainMenu.VersusMatchSettings.Mode.ToModel();

            if (mode.IsNetplay() || _netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Test)
            {
                /// We don't need Original ReplayRecorder in Netplay
                /// We can ignore this
                return;
            }

            orig(self);
        }
    }
}
