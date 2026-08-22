using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using TF.Replay.Domain.Extensions;
using TF.Replay.Domain.Utils;
using TowerFall;

namespace TF.Replay.Domain.CustomComponent
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
        private OutlineText _incomplete;
        private OutlineText _needsMigration;
        private Image[] _variants;

        private const float MatchLengthY = 52f;

        private const float VariantsY = 100f;

        private const float VariantIconSize = 14f;
        private const float VariantIconGap = 3f;

        private const float VariantsCenterX = -27f;
        private const float VariantsMaxWidth = 80f;
        private const float VariantsMinScale = 0.35f;
        private const int MaxVariantIcons = 12;

        private static Color WinBorder = Calc.HexToColor("FFE23F");

        private static Color LoseBorder = Calc.HexToColor("896B61");

        private static Color BlueTeam = Calc.HexToColor("4FA3E3");

        private static Color RedTeam = Calc.HexToColor("E3564F");

        private const float RosterMaxWidth = 180f;

        private const float TeammateGap = 2f;
        private const float SideGap = 24f;

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
                    if (playerName != null)
                    {
                        Remove(playerName);
                    }
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

            if (_incomplete != null)
            {
                Remove(_incomplete);
            }

            if (_needsMigration != null)
            {
                Remove(_needsMigration);
            }

            if (_variants != null)
            {
                foreach (var variant in _variants)
                {
                    Remove(variant);
                }
            }
        }

        public void UpdateInfo(Models.ReplayInfo replayInfo)
        {
            RemoveComponents();

            if (replayInfo.Archers.Count() == 0)
            {
                _logger.LogError("Replay {name} recorded no archers, cannot show it", replayInfo.Name);
                return;
            }

            var trial = TrialOf(replayInfo);

            UpdateMapIcon(replayInfo.Id, trial);
            UpdateTitle(replayInfo, trial);
            UpdateMode((Modes)replayInfo.Mode);
            UpdatePlayersPortrait(replayInfo);
            UpdateVs(replayInfo);
            UpdateReplayLength(replayInfo, trial);

            if (trial == null)
            {
                UpdateVariants(replayInfo);
            }

            var service = ServiceCollections.ResolveReplayService();
            var missingMod = service.MissingMod(replayInfo);

            if (missingMod != null)
            {
                UpdateMigration($"MISSING {missingMod}");
            }
            else if (service.NeedsMigration(replayInfo))
            {
                UpdateMigration("NEEDS MIGRATION");
            }
            else if (trial == null)
            {
                UpdateCompleteness(replayInfo);
            }
        }

        private void UpdateMigration(string message)
        {
            _needsMigration = new OutlineText(TFGame.Font, message.ToUpperInvariant());
            _needsMigration.Color = Color.Gold;
            _needsMigration.OutlineColor = Color.Black;
            _needsMigration.CenterOrigin();

            _needsMigration.Position.Y -= 22;

            Add(_needsMigration);
        }

        private void UpdateVariants(Models.ReplayInfo replayInfo)
        {
            var icons = ResolveVariantIcons(replayInfo.Variants).Take(MaxVariantIcons).ToArray();

            if (icons.Length == 0)
            {
                return;
            }

            _variants = new Image[icons.Length];

            var spacing = VariantIconSize + VariantIconGap;
            var scale = 1f;

            if (icons.Length * spacing > VariantsMaxWidth)
            {
                scale = Math.Max(VariantsMaxWidth / (icons.Length * spacing), VariantsMinScale);
                spacing *= scale;
            }

            var left = VariantsCenterX - (icons.Length - 1) * spacing / 2f;

            for (int i = 0; i < icons.Length; i++)
            {
                _variants[i] = new Image(icons[i]);
                _variants[i].CenterOrigin();
                _variants[i].Scale = Vector2.One * scale;
                _variants[i].Position.X = left + i * spacing;
                _variants[i].Position.Y = VariantsY;

                Add(_variants[i]);
            }
        }

        private static IEnumerable<Subtexture> ResolveVariantIcons(IEnumerable<string> names)
        {
            var set = TowerFall.MainMenu.VersusMatchSettings?.Variants;

            if (set == null || names == null)
            {
                yield break;
            }

            foreach (var name in names)
            {
                var variant = set.Variants.FirstOrDefault(candidate => candidate.Title == name)
                    ?? set.CustomVariants.FirstOrDefault(candidate => candidate.Value?.Title == name).Value;

                if (variant?.Icon != null)
                {
                    yield return variant.Icon;
                }
            }
        }

        private void UpdateCompleteness(Models.ReplayInfo replayInfo)
        {
            if (replayInfo.Archers.Any(archer => archer.HasWon))
            {
                return;
            }

            _incomplete = new OutlineText(TFGame.Font, "INCOMPLETE MATCH");
            _incomplete.Color = Color.Gold;
            _incomplete.OutlineColor = Color.Black;
            _incomplete.CenterOrigin();

            _incomplete.Position.Y -= 22;

            Add(_incomplete);
        }

        private void UpdateReplayLength(Models.ReplayInfo replayInfo, TrialsLevelData trial)
        {
            var text = trial != null && replayInfo.TrialsTimeTicks > 0
                ? TrialsResults.GetTimeString(replayInfo.TrialsTimeTicks)
                : replayInfo.MatchLength.ToString(@"mm\:ss");

            _matchLength = new OutlineText(TFGame.Font, text);
            _matchLength.Scale = new Vector2(1.5f);
            _matchLength.CenterOrigin();
            _matchLength.Position.Y += MatchLengthY;
            Add(_matchLength);
        }

        private void UpdateVs(Models.ReplayInfo replayInfo)
        {
            var twoSided = replayInfo.Archers.Count() == 2
                || (Modes)replayInfo.Mode == Modes.TeamDeathmatch;

            if (!twoSided)
            {
                return;
            }

            _vs = new OutlineText(TFGame.Font, "VS");
            _vs.Scale = new Vector2(2.0f);
            _vs.CenterOrigin();
            _vs.Position.Y += 20;
            Add(_vs);
        }

        private void UpdatePlayersPortrait(Models.ReplayInfo replayInfo)
        {
            var scored = (Modes)replayInfo.Mode != Modes.Trials;

            _portraits = new Image[replayInfo.Archers.Count()];
            _scores = scored ? new OutlineText[replayInfo.Archers.Count()] : null;
            _playerNames = new OutlineText[replayInfo.Archers.Count()];
            _portraitBg = new DrawRectangle[replayInfo.Archers.Count()];

            var archers = replayInfo.Archers.ToArray();
            var order = SeatOrder(replayInfo, archers.Length, out var teamOf);

            var taken = new HashSet<int>();

            foreach (var archer in archers)
            {
                if (ArcherDataExtensions.Exists(archer.Index, (int)archer.Type))
                {
                    taken.Add(archer.Index);
                }
            }

            var portraits = new Dictionary<int, Subtexture>();

            for (int seat = 0; seat < archers.Length; seat++)
            {
                portraits[seat] = ArcherPortrait(archers[seat], taken);
            }

            var native = portraits[order[0]].Width;

            var gaps = 0f;

            for (int i = 1; i < order.Length; i++)
            {
                gaps += teamOf[order[i]] >= 0 && teamOf[order[i]] == teamOf[order[i - 1]]
                    ? TeammateGap
                    : SideGap;
            }

            var scale = Math.Min(1f, (RosterMaxWidth - gaps) / (order.Length * native));
            var size = native * scale;
            var x = -(order.Length * size + gaps) / 2f + size / 2f;

            for (int slot = 0; slot < order.Length; slot++)
            {
                var i = order[slot];
                var archerInfo = archers[i];

                if (slot > 0)
                {
                    x += size + (teamOf[i] >= 0 && teamOf[i] == teamOf[order[slot - 1]]
                        ? TeammateGap
                        : SideGap);
                }

                _portraits[i] = new Image(portraits[i]);
                _portraits[i].CenterOrigin();
                _portraits[i].Scale = Vector2.One * scale;
                _portraits[i].Position.X += x;
                _portraits[i].Position.Y += 20;

                var half = size / 2f;
                var border = teamOf[i] switch
                {
                    0 => BlueTeam,
                    1 => RedTeam,
                    _ => archerInfo.HasWon ? WinBorder : LoseBorder,
                };

                _portraitBg[i] = new DrawRectangle(
                    _portraits[i].X - half - 1f, _portraits[i].Y - half - 1f,
                    size + 2f, size + 2f, border);

                Add(_portraitBg[i]);
                Add(_portraits[i]);

                if (scored)
                {
                    _scores[i] = new OutlineText(TFGame.Font, archerInfo.Score.ToString());
                    _scores[i].Scale = new Vector2(1.5f);
                    _scores[i].Color = Color.WhiteSmoke;
                    _scores[i].OutlineColor = Color.Transparent;
                    _scores[i].CenterOrigin();
                    _scores[i].Position.X = _portraits[i].Position.X;
                    _scores[i].Position.Y = _portraits[i].Position.Y + half + 8;
                    Add(_scores[i]);
                }

                var name = archerInfo.NetplayName?.ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                _playerNames[i] = new OutlineText(TFGame.Font, name);
                _playerNames[i].Color = Color.White;
                _playerNames[i].OutlineColor = Color.Transparent;
                _playerNames[i].CenterOrigin();
                _playerNames[i].Position.X = _portraits[i].Position.X;
                _playerNames[i].Position.Y = _portraits[i].Position.Y - half - 10;
                Add(_playerNames[i]);
            }
        }

        private static Subtexture ArcherPortrait(Models.ArcherInfo archerInfo, ISet<int> taken)
        {
            var (archerIndex, altIndex) = ArcherDataExtensions.EnsureArcherDataExist(archerInfo.Index, (int)archerInfo.Type, taken);

            var archerData = ArcherData.Get(archerIndex, (ArcherData.ArcherTypes)altIndex);

            return archerInfo.HasWon ? archerData.Portraits.Win : archerData.Portraits.Lose;
        }

        private static int[] SeatOrder(Models.ReplayInfo replayInfo, int count, out int[] teamOf)
        {
            var teams = new int[count];

            var recorded = replayInfo.Teams ?? [];
            var isTeamMode = (Modes)replayInfo.Mode == Modes.TeamDeathmatch;

            for (int seat = 0; seat < count; seat++)
            {
                teams[seat] = isTeamMode && seat < recorded.Length && recorded[seat] >= 0
                    ? recorded[seat]
                    : -1;
            }

            teamOf = teams;

            return Enumerable.Range(0, count)
                .OrderBy(seat => teams[seat] < 0 ? seat : teams[seat] * 100 + seat)
                .ToArray();
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
                case Modes.Trials:
                    _mode = new OutlineText(TFGame.Font, "TRIALS");
                    _mode.Color = QuestDifficultySelect.HardcoreColor;
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

        private static TrialsLevelData TrialOf(Models.ReplayInfo replayInfo)
        {
            if ((Modes)replayInfo.Mode != Modes.Trials)
            {
                return null;
            }

            var levels = GameData.TrialsLevels;

            if (replayInfo.Id < 0 || replayInfo.Id >= levels.GetLength(0) || replayInfo.TrialsLevelY < 0 || replayInfo.TrialsLevelY >= levels.GetLength(1))
            {
                return null;
            }

            return levels[replayInfo.Id, replayInfo.TrialsLevelY];
        }

        private void UpdateTitle(Models.ReplayInfo replayInfo, TrialsLevelData trial)
        {
            var name = trial != null
                ? $"{trial.Theme.Name} {replayInfo.TrialsLevelY + 1}"
                : GameData.VersusTowers[replayInfo.Id].Theme.Name;

            _title = new OutlineText(TFGame.Font, name?.ToUpperInvariant() ?? "");
            _title.Scale = Vector2.One * 1.4f;
            _title.Color = Color.WhiteSmoke;
            _title.OutlineColor = Color.Black;

            _title.Position.Y -= 38;

            Add(_title);
        }

        private void UpdateMapIcon(int mapId, TrialsLevelData trial)
        {
            var imgs = trial != null
                ? VersusGraphics.GetGraphics(trial.Theme)
                : VersusGraphics.GetVersusGraphics(mapId);

            block = imgs[0];
            icon = imgs[1];

            block.Position.Y -= 60;
            icon.Position.Y -= 60;

            Add(block);
            Add(icon);
        }
    }
}
