using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Patchs.Entity.HUD
{
    internal class VersusPlayerMatchResultsPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IInputService _inputService;

        public VersusPlayerMatchResultsPatch(INetplayManager netplayManager, IInputService inputService)
        {
            _netplayManager = netplayManager;
            _inputService = inputService;
        }

        public void Load()
        {
            On.TowerFall.VersusPlayerMatchResults.ctor += VersusPlayerMatchResults_ctor;
        }

        public void Unload()
        {
            On.TowerFall.VersusPlayerMatchResults.ctor -= VersusPlayerMatchResults_ctor;
        }

        private void VersusPlayerMatchResults_ctor(On.TowerFall.VersusPlayerMatchResults.orig_ctor orig, TowerFall.VersusPlayerMatchResults self, TowerFall.Session session, TowerFall.VersusMatchResults matchResults, int playerIndex, Vector2 tweenFrom, Vector2 tweenTo, List<TowerFall.AwardInfo> awards)
        {
            orig(self, session, matchResults, playerIndex, tweenFrom, tweenTo, awards);

            var dynVersusPlayerMatchResults = DynamicData.For(self);
            var gem = dynVersusPlayerMatchResults.Get<Sprite<string>>("gem");

            var netplayIndex = playerIndex;

            if (_netplayManager.ShouldSwapPlayer())
            {
                if (netplayIndex == 0)
                {
                    netplayIndex = _inputService.GetLocalPlayerInputIndex();
                }
                else
                {
                    netplayIndex = _inputService.GetRemotePlayerInputIndex();
                }
            }

            var playerName = netplayIndex == 0 ? _netplayManager.GetNetplayMeta().Name : _netplayManager.GetPlayer2Name();

            var playerNameText = new OutlineText(TFGame.Font, playerName, gem.Position + Vector2.UnitY * 15);
            playerNameText.Color = Color.White;
            var dynPlayerNameText = DynamicData.For(playerNameText);
            dynPlayerNameText.Add("IsPlayerName", true);
            self.Add(playerNameText);
        }
    }
}
