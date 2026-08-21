using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Context;
using TF.State.TowerFallExtensions;
using TowerFall;

namespace TF.State.Patchs.Component
{
    [HarmonyPatch(typeof(PlayerIndicator))]
    public class PlayerIndicatorStatePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Render")]
        public static bool PlayerIndicator_Render(PlayerIndicator __instance)
        {
            if (StateFlags.IsCaptureActive || StateFlags.IsReplayMode)
            {
                var dynPlayerIndcator = DynamicData.For(__instance);
                var colorSwitch = dynPlayerIndcator.Get<bool>("colorSwitch");
                var characterIndex = dynPlayerIndcator.Get<int>("characterIndex");
                var offset = dynPlayerIndcator.Get<Vector2>("offset");
                var entity = dynPlayerIndcator.Get<Monocle.Entity>("Entity");
                var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");
                var text = dynPlayerIndcator.Get<string>("text");
                var crown = dynPlayerIndcator.Get<bool>("crown");

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
            var dynPlayerIndcator = DynamicData.For(__instance);

            if (StateFlags.IsCaptureActive || StateFlags.IsReplayMode)
            {
                var sine = dynPlayerIndcator.Get<Monocle.SineWave>("sine");

                sine.UpdateAttributes(0.0f);
                dynPlayerIndcator.Set("colorSwitch", false);
            }
        }

    }
}
