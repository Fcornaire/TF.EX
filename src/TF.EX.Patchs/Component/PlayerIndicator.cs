using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Component
{
    [HarmonyPatch(typeof(PlayerIndicator))]
    public class PlayerIndicatorPatch
    {
        /// <summary>
        /// Same as original but with a small font size
        /// </summary>
        /// 
        [HarmonyPrefix]
        [HarmonyPatch("Render")]
        public static bool PlayerIndicator_Render(PlayerIndicator __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var dynPlayerIndcator = DynamicData.For(__instance);
            var colorSwitch = dynPlayerIndcator.Get<bool>("colorSwitch");
            var characterIndex = dynPlayerIndcator.Get<int>("characterIndex");
            var offset = dynPlayerIndcator.Get<Vector2>("offset");
            var entity = dynPlayerIndcator.Get<Monocle.Entity>("Entity");
            var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");
            var text = dynPlayerIndcator.Get<string>("text");
            var crown = dynPlayerIndcator.Get<bool>("crown");

            if (netplayManager.IsInit() || netplayManager.IsReplayMode())
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
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void PlayerIndicator_Update_Prefix(PlayerIndicator __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            var dynPlayerIndcator = DynamicData.For(__instance);

            if (netplayManager.IsInit() || netplayManager.IsReplayMode())
            {
                var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");

                sine.UpdateAttributes(0.0f);
                dynPlayerIndcator.Set("colorSwitch", false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void PlayerIndicator_Update_Postfix(PlayerIndicator __instance)
        {
            var dynPlayerIndcator = DynamicData.For(__instance);
            dynPlayerIndcator.Set("colorSwitch", false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch([typeof(Vector2), typeof(int), typeof(bool)])]
        public static void PlayerIndicator_ctor(PlayerIndicator __instance)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var dynPlayerIndcator = Traverse.Create(__instance);
            var text = dynPlayerIndcator.Field("text").GetValue<string>();
            var playerIndex = dynPlayerIndcator.Field("playerIndex").GetValue<int>();

            if (netplayManager.ShouldSwapPlayer())
            {
                if ((PlayerDraw)playerIndex == PlayerDraw.Player1)
                {
                    text = netplayManager.GetPlayer2Name();
                }
                else
                {
                    text = netplayManager.GetNetplayMeta().Name;
                }
            }
            else
            {
                if ((PlayerDraw)playerIndex == PlayerDraw.Player1)
                {
                    text = netplayManager.GetNetplayMeta().Name;
                }
                else
                {
                    text = netplayManager.GetPlayer2Name();
                }
            }

            dynPlayerIndcator.Field("text").SetValue(text);
        }
    }
}
