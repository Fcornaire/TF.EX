using Microsoft.Extensions.Logging;

namespace TF.Replay.Domain.Services
{
    public static class LegacyReplayMigration
    {
        public static void Run(ILogger logger)
        {
            var legacyFolder = ReplayService.GetLegacyReplaysFolder;

            try
            {
                if (!Directory.Exists(legacyFolder))
                {
                    return;
                }

                var moved = MoveContents(legacyFolder, ReplayService.ReplaysRootFolder);

                if (!Directory.EnumerateFileSystemEntries(legacyFolder).Any())
                {
                    Directory.Delete(legacyFolder);
                }

                if (moved > 0)
                {
                    logger?.LogInformation("Moved {count} legacy replay files", moved);
                }
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Could not migrate the legacy replays");
            }
        }

        private static int MoveContents(string source, string destination)
        {
            var moved = 0;

            foreach (var file in Directory.EnumerateFiles(source))
            {
                var folder = Path.GetExtension(file).Equals(".gif", StringComparison.OrdinalIgnoreCase) ? ReplayService.GifsRootFolder : destination;

                var target = Path.Combine(folder, Path.GetFileName(file));

                if (File.Exists(target))
                {
                    continue;
                }

                Directory.CreateDirectory(folder);
                File.Move(file, target);
                moved++;
            }

            foreach (var folder in Directory.EnumerateDirectories(source))
            {
                moved += MoveContents(folder, Path.Combine(destination, Path.GetFileName(folder)));

                if (!Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }

            return moved;
        }
    }
}
