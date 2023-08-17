using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class ReplayInfos : MenuItem
    {
        public static readonly Color SelectedColor = Calc.HexToColor("FFDA9B");

        private Replay _replay;

        private Action confirm;

        private Vector2 selected;

        private Vector2 tweenTo;

        private Vector2 tweenFrom;

        public Image image;

        public float selectionLerp;

        private ReplaysPanel _panel;
        private bool _isDisabled = false;

        public string _name = "";
        public string OriginalName => _replay.Informations.Name;

        public ReplayInfos(MainMenu mainMenu, Vector2 position, Replay replay, Action confirmAction) : base(position)
        {
            var dynSelf = DynamicData.For(this);
            dynSelf.Set("MainMenu", mainMenu);
            tweenTo = Position;
            tweenFrom = Position + Vector2.UnitX * -100f;
            selected = tweenTo + new Vector2(15f, 0f);
            this._replay = replay;

            this._name = string.Copy(_replay.Informations.Name);
            this._name = _name.Replace("T", " ");

            this.confirm = confirmAction;
            image = new Image(TFGame.MenuAtlas["ascension/slabTop"]);
            image.Origin.Y = image.Height / 2f;
            image.Scale = new Vector2(0.4f, 0.6f);
            Add(image);
        }

        public ReplayInfos(MainMenu mainMenu, Vector2 position, Replay replay, Action confirmAction, ReplaysPanel entity) : this(mainMenu, position, replay, confirmAction)
        {
            _panel = entity;
        }

        public override void Update()
        {
            base.Update();

            float num = selectionLerp;
            if (base.Selected)
            {
                selectionLerp = Math.Min(1f, selectionLerp + 0.25f * Engine.TimeMult);
            }
            else
            {
                selectionLerp = Math.Max(0f, selectionLerp - 0.1f * Engine.TimeMult);
            }

            if (num != selectionLerp)
            {
                image.Color = Color.Lerp(Color.White, SelectedColor, selectionLerp);
            }
        }
        
        public override void Render()
        {
            base.Render();

            Draw.TextRight(TFGame.Font, _name.ToUpper().Split('.')[0], Position + Vector2.UnitX * 100f, Color.WhiteSmoke);
        }

        public override void TweenIn()
        {
            Position = tweenFrom;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 20, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(tweenFrom, tweenTo, t.Eased);
            };
            Add(tween);
        }

        public override void TweenOut()
        {
            Vector2 start = Position;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 5, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(start, tweenFrom, t.Eased);
            };
            Add(tween);
        }

        protected override void OnConfirm()
        {
            if (_isDisabled)
            {
                return;
            }

            Sounds.ui_click.Play();
            _isDisabled = true;
            confirm();
        }


        protected override void OnSelect()
        {
            var mainMenu = TFGame.Instance.Scene as MainMenu;
            mainMenu.TweenUICameraToY(Math.Max(0f, base.Y - 200f));

            if (base.Y - 200f > 0)
            {
                Tween tweenProxi = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 12, start: true);
                Vector2 start = _panel.Position;
                Vector2 end = new Vector2(_panel.Position.X, Position.Y - 100f);

                tweenProxi.OnUpdate = delegate (Tween t)
                {
                    _panel.Position = Vector2.Lerp(start, end, t.Eased);
                };
                _panel.Add(tweenProxi);
            }

            _panel.UpdateInfo(_replay.Informations);

            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 10, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(tweenTo, selected, t.Eased);
            };
            Add(tween);
        }

        protected override void OnDeselect()
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 10, start: true);
            tween.OnUpdate = delegate (Tween t)
            {
                Position = Vector2.Lerp(selected, tweenTo, t.Eased);
            };
            Add(tween);
        }

    }
}
