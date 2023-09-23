using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using TF.EX.Common.Extensions;
using TF.EX.Domain.Extensions;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class ReplaysPanel : Entity
    {
        private readonly ILogger _logger;

        private Image block;

        private Image icon;

        private OutlineText _title;

        private OutlineText _mode;

        private Image[] _portraits;
        private OutlineText[] _scores;
        private OutlineText[] _playerNames;
        private DrawRectangle[] _portraitBg;
        private OutlineText _vs = new OutlineText(TFGame.Font, "VS");
        private OutlineText _matchLength;

        private static Color WinBorder = Calc.HexToColor("FFE23F");

        private static Color LoseBorder = Calc.HexToColor("896B61");

        public ReplaysPanel(float x, float y)
        {
            Position = new Vector2(x, y);
            _logger = ServiceCollections.ResolveLogger();
            Add(_vs);
            _vs.Visible = false;
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

            if (_portraits != null)
            {
                foreach (var portrait in _portraits)
                {
                    Remove(portrait);
                }
            }

            if (_scores != null)
            {
                foreach (var score in _scores)
                {
                    Remove(score);
                }
            }

            if (_playerNames != null)
            {
                foreach (var playerName in _playerNames)
                {
                    Remove(playerName);
                }
            }

            if (_portraitBg != null)
            {
                foreach (var portraitBg in _portraitBg)
                {
                    Remove(portraitBg);
                }
            }

            if (_vs != null)
            {
                Remove(_vs);
            }

            if (_matchLength != null)
            {
                Remove(_matchLength);
            }
        }

        public void UpdateInfo(TF.EX.Domain.Models.ReplayInfo replayInfo)
        {
            RemoveComponents();

            if (replayInfo.Archers.Count() == 0)
            {
                _logger.LogError<ReplaysPanel>($"Replay {replayInfo.Name} is from an older version )");
                return;
            }

            UpdateMapIcon(replayInfo.Id);
            UpdateTitle(replayInfo.Id);
            UpdateMode(replayInfo.Mode.ToTF());
            UpdatePlayersPortrait(replayInfo);
            UpdateVs();
            UpdateReplayLength(replayInfo.MatchLenght);
        }

        private void UpdateReplayLength(TimeSpan matchLenght)
        {
            _matchLength = new OutlineText(TFGame.Font, matchLenght.ToString(@"mm\:ss"));
            _matchLength.CenterOrigin();
            _matchLength.Position.Y += 70;
            Add(_matchLength);
        }

        private void UpdateVs()
        {
            //TODO: update position depending on the number of players

            _vs = new OutlineText(TFGame.Font, "VS");
            _vs.Scale = new Vector2(2.0f);
            _vs.CenterOrigin();
            _vs.Position.Y += 20;
            Add(_vs);
        }

        private void UpdatePlayersPortrait(Models.ReplayInfo replayInfo)
        {
            _portraits = new Image[replayInfo.Archers.Count()];
            _scores = new OutlineText[replayInfo.Archers.Count()];
            _playerNames = new OutlineText[replayInfo.Archers.Count()];
            _portraitBg = new DrawRectangle[replayInfo.Archers.Count()];

            var x = -50;

            for (int i = 0; i < replayInfo.Archers.Count(); i++)
            {
                var archerInfo = replayInfo.Archers.ElementAt(i);

                ArcherData archerData = ArcherData.Get(archerInfo.Index, (ArcherData.ArcherTypes)archerInfo.Type);
                if (archerInfo.HasWon)
                {
                    _portraits[i] = new Image(archerData.Portraits.Win);
                }
                else
                {
                    _portraits[i] = new Image(archerData.Portraits.Lose);
                }

                _portraits[i].CenterOrigin();
                _portraits[i].Position.X += x;
                _portraits[i].Position.Y += 20;

                _portraitBg[i] = new DrawRectangle(_portraits[i].X - _portraits[i].Width / 2f - 1f, _portraits[i].Y - _portraits[i].Height / 2f - 1f, _portraits[i].Width + 2f, _portraits[i].Height + 2f, archerInfo.HasWon ? WinBorder : LoseBorder);
                Add(_portraitBg[i]);
                Add(_portraits[i]);

                _scores[i] = new OutlineText(TFGame.Font, archerInfo.Score.ToString());
                _scores[i].Scale = new Vector2(1.5f);
                _scores[i].Color = Color.WhiteSmoke;
                _scores[i].OutlineColor = Color.Transparent;
                _scores[i].CenterOrigin();
                _scores[i].Position.X = _portraits[i].Position.X;
                _scores[i].Position.Y = _portraits[i].Position.Y + _portraits[i].Height / 2f + 10;
                Add(_scores[i]);

                _playerNames[i] = new OutlineText(TFGame.Font, archerInfo.NetplayName);
                _playerNames[i].Color = Color.White;
                _playerNames[i].OutlineColor = Color.Transparent;
                _playerNames[i].CenterOrigin();
                _playerNames[i].Position.X = _portraits[i].Position.X;
                _playerNames[i].Position.Y = _portraits[i].Position.Y - 30;
                Add(_playerNames[i]);

                x += 100;
            }
        }

        private void UpdateMode(Modes mode)
        {
            switch (mode)
            {
                case Modes.LastManStanding:
                    _mode = new OutlineText(TFGame.Font, "LAST MAN STANDING");
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

        private void UpdateTitle(int mapId)
        {
            var tower = GameData.VersusTowers[mapId];

            _title = new OutlineText(TFGame.Font, tower.Theme.Name);
            _title.Scale = Vector2.One * 1.4f;
            _title.Color = Color.WhiteSmoke;
            _title.OutlineColor = Color.Black;

            _title.Position.Y -= 38;

            Add(_title);
        }

        private void UpdateMapIcon(int mapId)
        {
            var imgs = MapButton.InitVersusGraphics(mapId);

            block = imgs[0];
            icon = imgs[1];

            block.Position.Y -= 60;
            icon.Position.Y -= 60;

            Add(block);
            Add(icon);
        }
    }
}
