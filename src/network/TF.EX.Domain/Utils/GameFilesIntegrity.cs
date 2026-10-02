using System.Security.Cryptography;

namespace TF.EX.Domain.Utils
{
    public static class GameFilesIntegrity
    {
        private const string DarkWorldRoot = "DarkWorldContent/";

        private static readonly string[] LevelFolders = ["Content/Levels/Versus", "DarkWorldContent/Levels/Versus"];
        private static readonly string[] LevelExtensions = [".oel", ".xml"];
        private const string SpriteDataFolder = "Content/Atlas/SpriteData";
        private static readonly string[] DataFiles = ["Content/Atlas/GameData/archerData.xml", "Content/Atlas/GameData/themeData.xml"];

        private static IReadOnlyList<string> _modified;

        public static IReadOnlyList<string> Modified => _modified ??= OperatingSystem.IsWindows() ? Scan(Directory.GetCurrentDirectory()) : []; //TODO: Other OS at some point

        public static bool IsModified => Modified.Count > 0;

        public static List<string> Scan(string gameRoot)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(gameRoot, "Content")))
                {
                    return [];
                }

                var expected = ReadManifest();

                if (!Directory.Exists(Path.Combine(gameRoot, DarkWorldRoot)))
                {
                    foreach (var path in expected.Keys.Where(path => path.StartsWith(DarkWorldRoot, StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        expected.Remove(path);
                    }
                }

                var local = LocalFiles(gameRoot).ToDictionary(path => path, path => Hash(Path.Combine(gameRoot, path)), StringComparer.OrdinalIgnoreCase);

                return [.. expected.Keys
                    .Union(local.Keys, StringComparer.OrdinalIgnoreCase)
                    .Where(path => !expected.TryGetValue(path, out var vanilla) || !local.TryGetValue(path, out var current) || vanilla != current)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];
            }
            catch (Exception)
            {
                return [];
            }
        }

        private static Dictionary<string, string> ReadManifest()
        {
            return GameFilesManifest.Vanilla
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split(' ', 2))
                .ToDictionary(parts => parts[1], parts => parts[0], StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> LocalFiles(string gameRoot)
        {
            foreach (var folder in LevelFolders.Where(folder => Directory.Exists(Path.Combine(gameRoot, folder))))
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(gameRoot, folder), "*", SearchOption.AllDirectories))
                {
                    if (LevelExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    {
                        yield return Relative(gameRoot, file);
                    }
                }
            }

            if (Directory.Exists(Path.Combine(gameRoot, SpriteDataFolder)))
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(gameRoot, SpriteDataFolder), "*.xml", SearchOption.TopDirectoryOnly))
                {
                    yield return Relative(gameRoot, file);
                }
            }

            foreach (var file in DataFiles.Where(file => File.Exists(Path.Combine(gameRoot, file))))
            {
                yield return file;
            }
        }

        private static string Relative(string gameRoot, string file)
        {
            return Path.GetRelativePath(gameRoot, file).Replace('\\', '/');
        }

        private static string Hash(string file)
        {
            var bytes = File.ReadAllBytes(file).Where(value => value != (byte)'\r').ToArray();

            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
    }
}
