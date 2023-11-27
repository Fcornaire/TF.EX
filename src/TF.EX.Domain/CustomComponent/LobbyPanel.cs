using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyPanel : Entity
    {
        private Image block;

        private Image icon;

        private List<Image> variantsImages = new List<Image>();

        private OutlineText _title;

        private OutlineText _mode;

        private Vector2 initialPosition;

        private MatchSettings.MatchLengths matchLength = MatchSettings.MatchLengths.Standard;

        private Color lengthColor = Calc.HexToColor("3CBCFC");

        private string[] lengthNames = new string[4] { "INSTANT MATCH", "QUICK MATCH", "STANDARD MATCH", "EPIC MATCH" };

        public Vector2 InitialPosition => initialPosition;

        public LobbyPanel(float x, float y) : base(-1)
        {
            Position = new Vector2(x, y);
            initialPosition = Position;
        }

        public override void Render()
        {
            if (TFGame.Instance.Scene != null && TFGame.Instance.Scene is MainMenu && (TFGame.Instance.Scene as MainMenu).State != TF.EX.Domain.Models.MenuState.LobbyBrowser.ToTFModel())
            {
                return;
            }

            var position = new Vector2(Position.X, Position.Y);
            position.Y -= 10;

            Draw.OutlineTextCentered(TFGame.Font, lengthNames[(int)matchLength], position + new Vector2(0f, -6f), lengthColor, 1f);

            base.Render();
        }

        public override void Removed()
        {
            base.Removed();
            RemoveComponents();
        }

        private void RemoveComponents()
        {
            if (block != null)
            {
                Remove(block);
            }

            if (icon != null)
            {
                Remove(icon);
            }

            if (_title != null)
            {
                Remove(_title);
            }

            if (_mode != null)
            {
                Remove(_mode);
            }

            foreach (var variant in variantsImages)
            {
                Remove(variant);
            }

            variantsImages.Clear();
        }

        public void UpdateInfo(Lobby lobby)
        {
            RemoveComponents();

            UpdateMapIcon(lobby.GameData.MapId);
            UpdateTitle(lobby.GameData.MapId);
            UpdateMode(lobby.GameData.Mode);
            UpdateVariant(lobby.GameData.Variants);
            UpdateMatchLength(lobby.GameData.MatchLength);
        }

        private void UpdateMatchLength(int matchLength)
        {
            this.matchLength = (MatchSettings.MatchLengths)matchLength;
        }

        private void UpdateMapIcon(int mapId)
        {
            var imgs = mapId == -1 ? MapButton.InitRandomVersusGraphics() : MapButton.InitVersusGraphics(mapId);

            if (block != null)
            {
                block.RemoveSelf();
            }

            if (icon != null)
            {
                icon.RemoveSelf();
            }

            block = imgs[0];
            icon = imgs[1];

            block.Position.Y -= 60;
            icon.Position.Y -= 60;

            Add(block);
            Add(icon);
        }


        private void UpdateTitle(int mapId)
        {
            var towerName = mapId == -1 ? "RANDOM" : TowerFall.GameData.VersusTowers[mapId].Theme.Name;

            _title = new OutlineText(TFGame.Font, towerName)
            {
                Scale = Vector2.One * 1.4f,
                Color = Color.WhiteSmoke,
                OutlineColor = Color.Black
            };

            _title.Position.Y -= 38;

            Add(_title);
        }

        private void UpdateMode(int mode)
        {
            var modeEnum = (TF.EX.Domain.Models.Modes)mode;

            switch (modeEnum.ToTF())
            {
                case Modes.LastManStanding:
                    _mode = new OutlineText(TowerFall.TFGame.Font, "LAST MAN STANDING");
                    _mode.Color = QuestDifficultySelect.LegendaryColor;
                    _mode.OutlineColor = Color.Transparent;
                    Add(_mode);
                    break;
                case Modes.HeadHunters:
                    _mode = new OutlineText(TFGame.Font, "HEADHUNTERS");
                    _mode.Color = QuestDifficultySelect.HardcoreColor;
                    _mode.OutlineColor = Color.Transparent;
                    Add(_mode);
                    break;
                case Modes.TeamDeathmatch:
                    _mode = new OutlineText(TFGame.Font, "TEAM DEATHMATCH");
                    _mode.Color = QuestDifficultySelect.NormalColor;
                    _mode.OutlineColor = Color.Transparent;
                    Add(_mode);
                    break;
                default:
                    _mode = new OutlineText(TFGame.Font, "LAST MAN STANDING ?");
                    _mode.Color = QuestDifficultySelect.LegendaryColor;
                    _mode.OutlineColor = Color.Transparent;
                    Add(_mode);
                    break;
            }

            _mode.Position.Y -= 30;
        }


        private void UpdateVariant(ICollection<string> variants)
        {
            var startX = -40;
            var startY = 0;

            foreach (string var in variants)
            {
                var variant = MainMenu.VersusMatchSettings.Variants.Variants.FirstOrDefault(v => v.Title == var);

                var sub = TFGame.MenuAtlas["controls/none"];

                if (variant != null)
                {
                    sub = variant.Icon;
                }

                var img = new Image(sub);
                img.CenterOrigin();
                img.Position.X = startX;
                img.Position.Y = startY;

                Add(img);

                variantsImages.Add(img);

                startX += 20;

                if (variantsImages.Count % 5 == 0)
                {
                    startX = -40;
                    startY += 20;
                }
            }
        }
    }

}

