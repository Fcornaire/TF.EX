using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Patchs
{
    internal class InputRendererPatch : IHookable
    {
        public void Load()
        {
            On.TowerFall.InputRenderer.ctor += InputRenderer_ctor;
            On.TowerFall.InputRenderer.Render += InputRenderer_Render;
        }

        public void Unload()
        {
            On.TowerFall.InputRenderer.ctor -= InputRenderer_ctor;
            On.TowerFall.InputRenderer.Render -= InputRenderer_Render;
        }

        private void InputRenderer_ctor(On.TowerFall.InputRenderer.orig_ctor orig, TowerFall.InputRenderer self, int playerIndex, float widthSoFar)
        {
            if (TFGame.Instance.Scene is LevelLoaderXML)
            {
                return;
            }

            orig(self, playerIndex, widthSoFar);

            var dynInputRender = DynamicData.For(self);
            var jump = dynInputRender.Get<Subtexture>("jump");
            var shoot = dynInputRender.Get<Subtexture>("shoot");
            var dodge = dynInputRender.Get<Subtexture>("dodge");
            //var bg = dynInputRender.Get<Rectangle>("bg");
            //var bg2 = dynInputRender.Get<Rectangle>("bg2");

            //TODO: Save controller to state to have the corresponding icon ?
            dynInputRender.Set("jump", TFGame.MenuAtlas["controls/xb360/a"]);
            dynInputRender.Set("shoot", TFGame.MenuAtlas["controls/xb360/x"]);
            dynInputRender.Set("dodge", TFGame.MenuAtlas["controls/xb360/rt"]);
            Subtexture move = TFGame.MenuAtlas["controls/xb360/stick"];
            dynInputRender.Add("move", move);

            self.Width = 12 + move.Width + jump.Width + shoot.Width + dodge.Width + 12;
            Vector2 vector = new Vector2(widthSoFar + (float)(self.Width / 2), 226f);
            Vector2 moveAt = vector + Vector2.UnitX * (-self.Width / 2 + 6 + move.Width / 2);
            dynInputRender.Set("jumpAt", vector + Vector2.UnitX * (-self.Width / 2 + 25 + jump.Width / 2));
            dynInputRender.Set("shootAt", vector + Vector2.UnitX * (self.Width / 2 - 25 - shoot.Width / 2));
            dynInputRender.Set("dodgeAt", vector + Vector2.UnitX * (self.Width / 2 - 6 - dodge.Width / 2));

            Vector2 moveAtReference = moveAt;

            var bg = default(Rectangle);
            bg.X = (int)(vector.X - (float)(self.Width / 2) + 6f - 2f);
            bg.Width = self.Width - 12 + 4;
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

        private void InputRenderer_Render(On.TowerFall.InputRenderer.orig_Render orig, InputRenderer self, InputState state)
        {
            orig(self, state);

            var dynInputRender = DynamicData.For(self);
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
                    extraX = Vector2.UnitX * 3;
                    break;
                case -1:
                    extraX = -Vector2.UnitX * 3;
                    break;
            }

            switch (state.MoveY)
            {
                case 0:
                    extraY = Vector2.Zero;
                    break;
                case 1:
                    extraY = Vector2.UnitX * 3;
                    break;
                case -1:
                    extraY = -Vector2.UnitX * 3;
                    break;
            }

            moveAt += extraX;
            moveAt += extraY;

            Draw.TextureCentered(move, moveAt, state.MoveX != 0 || state.MoveY != 0 ? Color.White : dynInputRender.Get<Color>("color"));
        }
    }
}
