using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Component
{
    public class PlayerIndicatorPatch : IHookable
    {
        public INetplayManager _netplayManager;

        public PlayerIndicatorPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.TowerFall.PlayerIndicator.Update += PlayerIndicator_Update;
            On.TowerFall.PlayerIndicator.ctor += PlayerIndicator_ctor;
            On.TowerFall.PlayerIndicator.Render += PlayerIndicator_Render;
        }

        public void Unload()
        {
            On.TowerFall.PlayerIndicator.Update -= PlayerIndicator_Update;
            On.TowerFall.PlayerIndicator.ctor -= PlayerIndicator_ctor;
            On.TowerFall.PlayerIndicator.Render -= PlayerIndicator_Render;
        }

        /// <summary>
        /// Same as original but with a small font size
        /// </summary>
        private void PlayerIndicator_Render(On.TowerFall.PlayerIndicator.orig_Render orig, TowerFall.PlayerIndicator self)
        {
            var dynPlayerIndcator = DynamicData.For(self);
            var colorSwitch = dynPlayerIndcator.Get<bool>("colorSwitch");
            var characterIndex = dynPlayerIndcator.Get<int>("characterIndex");
            var offset = dynPlayerIndcator.Get<Vector2>("offset");
            var entity = dynPlayerIndcator.Get<Monocle.Entity>("Entity");
            var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");
            var text = dynPlayerIndcator.Get<string>("text");
            var crown = dynPlayerIndcator.Get<bool>("crown");

            if (_netplayManager.IsInit() || _netplayManager.IsReplayMode())
            {
                Color color = (colorSwitch ? ArcherData.Archers[characterIndex].ColorB : ArcherData.Archers[characterIndex].ColorA);
                Vector2 vector = entity.Position + offset + new Vector2(0f, -32f);
                vector.Y = Math.Max(10f, vector.Y);
                vector.Y += sine.Value * 3f;
                _ = TFGame.Font.MeasureString(text) * 2f;
                if (crown)
                {
                    Draw.OutlineTextureCentered(TFGame.Atlas["versus/crown"], vector + new Vector2(0f, -12f), Color.White);
                }

                Draw.OutlineTextCentered(TFGame.Font, text, vector + new Vector2(1f, 0f), color, 1.2f);
                Draw.OutlineTextureCentered(TFGame.Atlas["versus/playerIndicator"], vector + new Vector2(0f, 8f), color);
            }
            else
            {
                orig(self);
            }
        }

        private void PlayerIndicator_Update(On.TowerFall.PlayerIndicator.orig_Update orig, TowerFall.PlayerIndicator self)
        {
            var dynPlayerIndcator = DynamicData.For(self);

            if (_netplayManager.IsInit() || _netplayManager.IsReplayMode())
            {
                var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");

                sine.UpdateAttributes(0.0f);
                dynPlayerIndcator.Set("colorSwitch", false);
            }

            orig(self);

            dynPlayerIndcator.Set("colorSwitch", false);
        }

        private void PlayerIndicator_ctor(On.TowerFall.PlayerIndicator.orig_ctor orig, TowerFall.PlayerIndicator self, Vector2 offset, int playerIndex, bool crown)
        {
            orig(self, offset, playerIndex, crown);

            var dynPlayerIndcator = DynamicData.For(self);
            var text = dynPlayerIndcator.Get<string>("text");

            if (_netplayManager.ShouldSwapPlayer())
            {
                if ((PlayerDraw)playerIndex == PlayerDraw.Player1)
                {
                    text = _netplayManager.GetPlayer2Name();
                }
                else
                {
                    text = _netplayManager.GetConfig().Name;
                }
            }
            else
            {
                if ((PlayerDraw)playerIndex == PlayerDraw.Player1)
                {
                    text = _netplayManager.GetConfig().Name;
                }
                else
                {
                    text = _netplayManager.GetPlayer2Name();
                }
            }

            dynPlayerIndcator.Set("text", text);
        }
    }
}
