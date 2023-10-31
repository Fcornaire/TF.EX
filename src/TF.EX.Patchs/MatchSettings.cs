using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Patchs
{
    internal class MatchSettingsPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;

        public MatchSettingsPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.MatchSettings.PlayerGoals += MatchSettings_PlayerGoals;
        }

        public void Unload()
        {
            On.TowerFall.MatchSettings.PlayerGoals -= MatchSettings_PlayerGoals;
        }

        private int MatchSettings_PlayerGoals(On.TowerFall.MatchSettings.orig_PlayerGoals orig, MatchSettings self, int p2goal, int p3goal, int p4goal)
        {
            if (_netplayManager.IsTestMode())
            {
                return 2;
            }

            if (_netplayManager.GetNetplayMode() == Domain.Models.NetplayMode.Local)
            {
                return 10;
            }

            return orig(self, p2goal, p3goal, p4goal);
        }
    }
}
