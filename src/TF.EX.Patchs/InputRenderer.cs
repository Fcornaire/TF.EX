using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Patchs
{
    [HarmonyPatch(typeof(InputRenderer))]
    internal class InputRendererPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, [typeof(int), typeof(float)])]
        public static bool InputRenderer_ctor_Prefix()
        {
            if (TFGame.Instance.Scene is LevelLoaderXML)
            {
                return false;
            }

            return true;
        }

        private static void EnsureSubtexture(InputRenderer inputRenderer)
        {
            var dynInputRender = DynamicData.For(inputRenderer);
            var jump = dynInputRender.Get<Subtexture>("jump");
            var shoot = dynInputRender.Get<Subtexture>("shoot");
            var dodge = dynInputRender.Get<Subtexture>("dodge");
            if (jump == null || shoot == null || dodge == null)
            {
                dynInputRender.Set("jump", TFGame.PlayerInputs[0].JumpIcon);
                dynInputRender.Set("shoot", TFGame.PlayerInputs[0].ShootIcon);
                dynInputRender.Set("dodge", TFGame.PlayerInputs[0].DodgeIcon);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(MethodType.Constructor, [typeof(int), typeof(float)])]
        public static void InputRenderer_ctor_Postfix(InputRenderer __instance, float widthSoFar)
        {
            EnsureSubtexture(__instance);
            var dynInputRender = DynamicData.For(__instance);
            var jump = dynInputRender.Get<Subtexture>("jump");
            var shoot = dynInputRender.Get<Subtexture>("shoot");
            var dodge = dynInputRender.Get<Subtexture>("dodge");

            //TODO: Save controller to state to have the corresponding icon ?
            dynInputRender.Set("jump", TFGame.MenuAtlas["controls/xb360/a"]);
            dynInputRender.Set("shoot", TFGame.MenuAtlas["controls/xb360/x"]);
            dynInputRender.Set("dodge", TFGame.MenuAtlas["controls/xb360/rt"]);
            Subtexture move = TFGame.MenuAtlas["controls/xb360/stick"];
            dynInputRender.Add("move", move);

            __instance.Width = 12 + move.Width + jump.Width + shoot.Width + dodge.Width + 12;
            Vector2 vector = new Vector2(widthSoFar + (float)(__instance.Width / 2), 226f);
            Vector2 moveAt = vector + Vector2.UnitX * (-__instance.Width / 2 + 6 + move.Width / 2);
            dynInputRender.Set("jumpAt", vector + Vector2.UnitX * (-__instance.Width / 2 + 25 + jump.Width / 2));
            dynInputRender.Set("shootAt", vector + Vector2.UnitX * (__instance.Width / 2 - 25 - dodge.Width + 15 - shoot.Width / 2));
            dynInputRender.Set("dodgeAt", vector + Vector2.UnitX * (__instance.Width / 2 - 6 - dodge.Width / 2));

            Vector2 moveAtReference = moveAt;

            var bg = default(Rectangle);
            bg.X = (int)(vector.X - (float)(__instance.Width / 2) + 6f - 2f);
            bg.Width = __instance.Width - 12 + 4;
            bg.Y = (int)(vector.Y - 2f);
            bg.Height = 11;
            var bg2 = default(Rectangle);
            bg2.X = bg.X;
            bg2.Width = bg.Width;
            bg2.Y = bg.Y + 10;
            bg2.Height = 2;

            dynInputRender.Set("bg", bg);
            dynInputRender.Set("bg2", bg2);
            dynInputRender.Add("moveAt", moveAt);
            dynInputRender.Add("moveAtReference", moveAtReference);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Render")]
        public static void InputRenderer_Render(InputRenderer __instance, InputState state)
        {
            var dynInputRender = DynamicData.For(__instance);
            var move = dynInputRender.Get<Subtexture>("move");
            var moveAt = dynInputRender.Get<Vector2>("moveAt");
            var moveAtReference = dynInputRender.Get<Vector2>("moveAtReference");

            dynInputRender.Set("moveAt", moveAtReference);

            Vector2 extraX = Vector2.Zero;
            Vector2 extraY = Vector2.Zero;

            switch (state.MoveX)
            {
                case 0:
                    extraX = Vector2.Zero;
                    break;
                case 1:
                    extraX = Vector2.UnitX * 4;
                    break;
                case -1:
                    extraX = -Vector2.UnitX * 4;
                    break;
            }

            switch (state.MoveY)
            {
                case 0:
                    extraY = Vector2.Zero;
                    break;
                case 1:
                    extraY = Vector2.UnitY * 4;
                    break;
                case -1:
                    extraY = -Vector2.UnitY * 4;
                    break;
            }

            moveAt += extraX;
            moveAt += extraY;

            Draw.TextureCentered(move, moveAt, state.MoveX != 0 || state.MoveY != 0 ? Color.White : dynInputRender.Get<Color>("color"));
        }
    }
}
