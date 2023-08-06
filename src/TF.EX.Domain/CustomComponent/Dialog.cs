using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    //TODO: Make this a proper menu
    public class Dialog : Monocle.Entity
    {
        private Action _cancel;
        private Counter confirmCounter;
        private List<string> optionNames = new List<string>();
        private List<string> selectedOptionNames = new List<string>();
        private List<Action> optionActions = new List<Action>();
        private Wiggler selectionWiggler;
        private string title;
        private MenuPanel panel;
        private bool selectionFlash;
        private int optionIndex;
        private string dialogText;
        private bool _withAction => _actions.Any();
        private bool _withCenterAction => _centerAction != null;
        private bool _noOrb = false;
        private bool _isDisabled = false;

        private Func<string> _centerAction;
        private Dictionary<string, Action> _actions = new Dictionary<string, Action>();

        public Sprite<int> sprite;

        private readonly Color SelectionA = Monocle.Calc.HexToColor("F87858");

        private readonly Color SelectionB = Monocle.Calc.HexToColor("F7BB59");

        private readonly Color NotSelection = Monocle.Calc.HexToColor("F8F8F8");

        public Dialog(string title, string dialogText, Vector2 position, Action cancel, Dictionary<string, Action> actions, Func<string> centerAction = null, bool noOrb = false) : base(position, 4)
        {
            var dynDialog = DynamicData.For(this);
            dynDialog.Set("Scene", TFGame.Instance.Scene);
            dynDialog.Set("depth", -10000000);

            _centerAction = centerAction;
            _actions = actions;
            _cancel = cancel;
            _noOrb = noOrb;

            confirmCounter = new Counter();
            confirmCounter.Set(10);

            Add(panel = new MenuPanel(250, 120));

            if (_withAction)
            {
                selectionWiggler = Wiggler.Create(20, 4f);
                Add(selectionWiggler);

                foreach (var action in _actions)
                {
                    AddItem(action.Key.ToUpper(), action.Value);
                }
            }
            else
            {
                if (_cancel != null)
                {
                    AddItem("CANCEL", _cancel);
                }
            }

            this.title = title;

            this.dialogText = dialogText;

            if (!noOrb && !_withAction)
            {
                sprite = TFGame.SpriteData.GetSpriteInt("ChaosOrb");
                sprite.Play(0);
                sprite.Scale = Vector2.One * 1.5f;
                sprite.Rate = 1.5f;
                Add(sprite);
            }

        }

        private void AddItem(string name, Action action)
        {
            optionNames.Add(name);
            selectedOptionNames.Add("> " + name);
            optionActions.Add(action);
        }

        public void Resume()
        {
            RemoveSelf();
            Visible = false;
        }

        public override void Removed()
        {
            base.Removed();
            MenuInput.Clear();
        }

        public override void SceneEnd()
        {
            base.SceneEnd();
        }

        public override void Update()
        {
            base.Update();
            if (_withAction)
            {
                if (base.Scene.OnInterval(4))
                {
                    selectionFlash = !selectionFlash;
                }

                MenuInput.Update();
                if (MenuInput.Right)
                {
                    if (optionIndex < optionNames.Count - 1)
                    {
                        Sounds.ui_move1.Play();
                        optionIndex++;
                        selectionWiggler.Start();
                    }
                }
                else if (MenuInput.Left)
                {
                    if (optionIndex > 0)
                    {
                        Sounds.ui_move1.Play();
                        optionIndex--;
                        selectionWiggler.Start();
                    }
                }
                else if ((bool)confirmCounter)
                {
                    confirmCounter.Update();
                }
                else if (MenuInput.Confirm && !_isDisabled)
                {
                    optionActions[optionIndex]();
                }
            }
            else
            {
                if ((MenuInput.Confirm || MenuInput.Back) && _cancel != null)
                {
                    _cancel();
                }
            }
        }

        public override void Render()
        {
            Draw.Rect(0f, 0f, 320f, 240f, Color.Black * 0.5f);
            if (!_noOrb && !_withAction)
            {
                sprite.DrawOutline();
            }

            base.Render();
            Vector2 vector = Position + new Vector2(0f, (0f - panel.Height) / 2f - 8f);

            if (title != "")
            {
                Draw.TextureCentered(TFGame.MenuAtlas["questResults/arrow"], vector + new Vector2(0f, -2f), Color.White);
                Draw.OutlineTextCentered(TFGame.Font, title.ToUpper(), vector, Color.White, 1.5f);
            }

            if (_withCenterAction)
            {
                var pingStr = _centerAction();
                int latency;

                var color = Color.White;
                if (Int32.TryParse(pingStr.Split(' ')[0], out latency))
                {
                    switch (latency)
                    {
                        case var n when (n >= 0 && n < 60):
                            color = Color.LightGreen;
                            break;
                        case var n when (n >= 60 && n < 120):
                            color = Color.GreenYellow;
                            break;
                        case var n when (n >= 120 && n < 150):
                            color = Color.OrangeRed;
                            break;
                        case var n when (n >= 150):
                            color = Color.Red;
                            break;
                        default:
                            break;
                    }
                    Draw.OutlineTextCentered(TFGame.Font, pingStr, Position, color, 1.5f);

                    _isDisabled = (latency == 0);
                }
                else
                {
                    Draw.OutlineTextCentered(TFGame.Font, _centerAction().ToUpper(), Position, NotSelection, 1.5f);
                }
            }

            if (_withAction)
            {
                Vector2 vectorText = vector + new Vector2(0f, 25f);
                Draw.OutlineTextCentered(TFGame.Font, dialogText.ToUpper(), vectorText, NotSelection, 1.5f);

                int num = (optionNames.Count - 1) * 14 + 20 + 20 - 1;
                Vector2 vector2 = new Vector2(base.X - (float)(num / 2) - 40, base.Y + 40);
                for (int i = 0; i < optionNames.Count; i++)
                {
                    Vector2 zero = Vector2.Zero;
                    if (i == optionIndex)
                    {
                        zero.X = selectionWiggler.Value * 3f;
                    }

                    Draw.TextCentered(color: (optionIndex != i) ? NotSelection : (selectionFlash ? SelectionB : SelectionA), font: TFGame.Font, text: (optionIndex == i) ? selectedOptionNames[i] : optionNames[i], position: vector2 + zero);
                    vector2.X += 155;
                }
            }
            else
            {
                Vector2 vectorText = Position + new Vector2(0f, -25f);
                Draw.OutlineTextCentered(TFGame.Font, dialogText.ToUpper(), vectorText, NotSelection, 1.5f);
                if (optionNames.Count == 1)
                {
                    Draw.OutlineTextCentered(font: TFGame.Font, text: optionNames[0], position: vectorText + new Vector2(0f, 70f), Color.OrangeRed, 1.2f);
                }
            }
        }
    }
}
