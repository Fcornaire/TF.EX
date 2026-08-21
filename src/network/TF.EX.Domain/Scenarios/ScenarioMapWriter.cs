using System.Text;
using System.Text.RegularExpressions;

namespace TF.EX.Domain.Scenarios
{
    public static class ScenarioMapWriter
    {
        public const string OutputDirectory = "EXScenarios";

        public const int Columns = 32;
        public const int Rows = 24;
        public const int TileSize = 10;

        public const int FloorY = 210;
        private const int FloorRow = 21;
        private const int CeilingRows = 2;

        public const int SpawnY = FloorY - TileSize;

        private static readonly string TemplatePath = Path.Combine("Content", "Levels", "Versus", "00 - Sacred Ground", "08.oel");

        public static readonly (int X, int Y)[] FlatSpawns = [(60, SpawnY), (260, SpawnY), (110, SpawnY), (210, SpawnY)];

        private static readonly Regex EntitiesBlock = new Regex(@"<Entities>.*?</Entities>", RegexOptions.Singleline);
        private static readonly Regex SolidsBlock = new Regex(@"<Solids[^>]*>.*?</Solids>", RegexOptions.Singleline);
        private static readonly Regex BgBlock = new Regex(@"<BG exportMode[^>]*>.*?</BG>", RegexOptions.Singleline);
        private static readonly Regex BgTilesBlock = new Regex(@"(<BGTiles[^>]*>).*?(</BGTiles>)", RegexOptions.Singleline);

        public static string Write(Scenario scenario)
        {
            var oel = File.ReadAllText(Path.GetFullPath(TemplatePath));

            oel = SolidsBlock.Replace(oel, BuildFlatSolids(scenario), 1);
            oel = BgBlock.Replace(oel, BuildEmptyBg(), 1);
            oel = BgTilesBlock.Replace(oel, "$1$2", 1);

            oel = EntitiesBlock.Replace(oel, BuildEntities(scenario), 1);

            Directory.CreateDirectory(OutputDirectory);

            var path = Path.GetFullPath(Path.Combine(OutputDirectory, $"{scenario.Name}.oel"));

            File.WriteAllText(path, oel);

            return path;
        }

        private static string BuildFlatSolids(Scenario scenario)
        {
            var grid = new char[Rows][];

            for (int row = 0; row < Rows; row++)
            {
                var solid = row < CeilingRows || row >= FloorRow;

                grid[row] = new string(solid ? '1' : '0', Columns).ToCharArray();
            }

            foreach (var platform in scenario.Platforms)
            {
                var row = platform.Y / TileSize;

                if (row < 0 || row >= Rows)
                {
                    continue;
                }

                for (int x = platform.X; x < platform.X + platform.Width; x += TileSize)
                {
                    var column = x / TileSize;

                    if (column >= 0 && column < Columns)
                    {
                        grid[row][column] = '1';
                    }
                }
            }

            var builder = new StringBuilder();

            builder.Append("<Solids exportMode=\"Bitstring\">");

            for (int row = 0; row < Rows; row++)
            {
                builder.Append(new string(grid[row]));

                if (row < Rows - 1)
                {
                    builder.AppendLine();
                }
            }

            builder.Append("</Solids>");

            return builder.ToString();
        }

        private static string BuildEmptyBg()
        {
            var builder = new StringBuilder();

            builder.Append("<BG exportMode=\"Bitstring\">");

            for (int row = 0; row < Rows; row++)
            {
                builder.Append(new string('0', Columns));

                if (row < Rows - 1)
                {
                    builder.AppendLine();
                }
            }

            builder.Append("</BG>");

            return builder.ToString();
        }

        private static string BuildEntities(Scenario scenario)
        {
            var builder = new StringBuilder();
            var id = 0;

            builder.AppendLine("<Entities>");

            var seats = ResolveSpawns(scenario).ToList();
            var teamA = seats.Where((_, i) => i % 2 == 0).ToList();
            var teamB = seats.Where((_, i) => i % 2 != 0).ToList();

            for (int i = 0; i < Math.Max(teamA.Count, teamB.Count); i++)
            {
                if (i < teamA.Count)
                {
                    Append(builder, ref id, "TeamSpawnA", teamA[i].X, teamA[i].Y);
                }

                if (i < teamB.Count)
                {
                    Append(builder, ref id, "TeamSpawnB", teamB[i].X, teamB[i].Y);
                }
            }

            foreach (var spawn in ResolveSpawns(scenario))
            {
                Append(builder, ref id, "PlayerSpawn", spawn.X, spawn.Y);
            }

            foreach (var entity in scenario.Entities)
            {
                Append(builder, ref id, entity.Name, entity.x, entity.y, entity.Attributes, entity.Nodes);
            }

            builder.Append("  </Entities>");

            return builder.ToString();
        }

        private static IEnumerable<(int X, int Y)> ResolveSpawns(Scenario scenario)
        {
            return scenario.Spawns.Length > 0 ? scenario.Spawns : FlatSpawns;
        }

        private static void Append(StringBuilder builder, ref int id, string name, int x, int y, string attributes = "", (int X, int Y)[] nodes = null)
        {
            var extra = string.IsNullOrEmpty(attributes) ? "" : " " + attributes;

            if (nodes == null || nodes.Length == 0)
            {
                builder.AppendLine($"    <{name} id=\"{id}\" x=\"{x}\" y=\"{y}\"{extra} />");
                id++;

                return;
            }

            builder.AppendLine($"    <{name} id=\"{id}\" x=\"{x}\" y=\"{y}\"{extra}>");

            foreach (var node in nodes)
            {
                builder.AppendLine($"      <node x=\"{node.X}\" y=\"{node.Y}\" />");
            }

            builder.AppendLine($"    </{name}>");
            id++;
        }
    }
}
