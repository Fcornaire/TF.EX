using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    //Same as vanilla but smaller
    public class CompactCoOpButton : MainModeButton
    {
        private const float CompactScale = 0.65f;
        private const float OrbitRadius = 30f * CompactScale;
        private const float OrbitWave = 5f * CompactScale;

        private readonly Sprite<int> portal;
        private readonly Image[] skulls;
        private readonly SineWave skullSine;
        private float skullAlpha;
        private float skullPosAngle;
        private float lastSkullDistance;
        private bool confirmed;

        public override float BaseScale => CompactScale;
        public override float BaseTextScale => 1f;
        public override float AddYMult => 0.65f;
        public override bool Rotate => false;
        public override bool PlaySound => false;

        public override float ImageScale
        {
            get { return portal.Scale.X; }
            set { portal.Scale = Vector2.One * value; }
        }

        public override float ImageRotation
        {
            get { return portal.Rotation; }
            set { portal.Rotation = value; }
        }

        public override float ImageY
        {
            get { return portal.Y; }
            set { portal.Y = value; }
        }

        public CompactCoOpButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, "CO-OP", "")
        {
            portal = TFGame.MenuSpriteData.GetSpriteInt("CoopIcon");
            portal.Play(0);
            Add(portal);

            skulls = new Image[4];
            for (int i = 0; i < skulls.Length; i++)
            {
                var skull = new Image(new Subtexture(TFGame.MenuAtlas["coopSkulls"], i * 19, 0, 19, 19));
                skull.CenterOrigin();
                skull.Scale = Vector2.One * CompactScale;
                Add(skull);
                skulls[i] = skull;
            }

            skullSine = new SineWave(120);
            Add(skullSine);
            skullPosAngle = Calc.Random.NextAngle();
            skullAlpha = 0f;
            UpdateSkulls();
        }

        private void UpdateSkulls()
        {
            skullPosAngle += MathHelper.Pi / 360f * Engine.TimeMult;
            skullAlpha = Calc.Approach(skullAlpha, Selected || confirmed ? 1f : 0.5f, 0.02f * Engine.TimeMult);
            lastSkullDistance = OrbitRadius + OrbitWave * skullSine.Value * skullAlpha;

            var vector = Calc.AngleToVector(skullPosAngle, lastSkullDistance);
            skulls[0].Position = portal.Position + vector;
            skulls[1].Position = portal.Position + vector.Perpendicular();
            skulls[2].Position = portal.Position + -vector;
            skulls[3].Position = portal.Position + -vector.Perpendicular();

            foreach (var skull in skulls)
            {
                skull.Color = Color.White * skullAlpha;
                skull.Rotation = skullSine.TwoValue * 20f * (MathHelper.Pi / 180f);
            }
        }

        public override void Update()
        {
            if (!confirmed)
            {
                UpdateSkulls();
            }

            base.Update();
        }

        private IEnumerator ConfirmSequence()
        {
            for (int i = 0; i < skulls.Length; i++)
            {
                var startPosAngle = skullPosAngle + MathHelper.PiOver2 * i;
                var startRotation = skulls[i].Rotation;
                var skull = skulls[i];

                var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 40, start: true);
                tween.OnUpdate = t =>
                {
                    var angle = MathHelper.Lerp(startPosAngle, startPosAngle + MathHelper.TwoPi, t.Eased);
                    var length = MathHelper.Lerp(lastSkullDistance, 0f, t.Eased);
                    skull.Rotation = MathHelper.Lerp(startRotation, startRotation + MathHelper.TwoPi, t.Eased);
                    skull.Position = portal.Position + Calc.AngleToVector(angle, length);
                    skull.Scale = Vector2.One * MathHelper.Lerp(CompactScale, 0f, t.Eased);
                };
                Add(tween);

                yield return 3;
            }
        }

        protected override void OnConfirm()
        {
            confirmed = true;
            Add(new Coroutine(ConfirmSequence()));
            base.OnConfirm();
        }

        protected override void MenuAction()
        {
            base.MainMenu.State = MainMenu.MenuState.CoOp;
        }

        public override void Render()
        {
            portal.DrawOutline();

            if (skullAlpha >= 1f)
            {
                foreach (var skull in skulls)
                {
                    skull.DrawOutline();
                }
            }

            base.Render();
        }
    }
}
