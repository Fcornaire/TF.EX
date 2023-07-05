using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace TF.EX.Common
{
    internal class Meta
    {
        public string Version { get; set; }
    }

    public interface IAutoUpdater
    {
        Task CheckForUpdate();
        bool IsUpdateAvailable();
        bool Update();
    }

    public class AutoUpdater : IAutoUpdater
    {
        private string DownloadPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TF.EX", "Update");
        private string UpdatePath => $"{DownloadPath}/TF.EX";

        private string ZipPath => $"{DownloadPath}/update.zip";

        private bool _isUpdateAvailable = false;

        public async Task CheckForUpdate()
        {
            try
            {
                var meta = File.ReadAllText(".\\Mods\\TF.EX\\meta.json");
                var version = GetVersion(meta);

                Console.WriteLine($"TF.EX Mod current version {version}");

                Console.WriteLine("Checking for updates...");
                await DownloadLatest();

                ExtractAndCheckIfUpdatable(version);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while trying to check for Update: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void ExtractAndCheckIfUpdatable(string currentVersion)
        {
            try
            {
                if (!Directory.Exists(DownloadPath))
                {
                    Console.WriteLine("No update found");
                    return;
                }

                if (!File.Exists(ZipPath))
                {
                    Console.WriteLine("No update found");
                    return;
                }

                Console.WriteLine("Extracting update...");

                ZipFile.ExtractToDirectory(ZipPath, DownloadPath);

                if (!Directory.Exists(UpdatePath))
                {
                    Console.WriteLine("No update found");
                    return;
                }

                var metaDownload = File.ReadAllText(Path.Combine(UpdatePath, "meta.json"));
                var newVersion = GetVersion(metaDownload);

                if (IsUpdateAvailable(currentVersion, newVersion))
                {
                    _isUpdateAvailable = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while trying to extract update: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public bool Update()
        {
            if (!_isUpdateAvailable)
            {
                Console.WriteLine("No update found");
                return false;
            }

            var updateFiles = Directory.GetFiles(UpdatePath);

            foreach (var file in updateFiles)
            {
                var fileName = Path.GetFileName(file);
                var destination = $"{Directory.GetCurrentDirectory()}\\Mods\\TF.EX\\{fileName}";

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(file, destination);

            }

            Directory.Delete(UpdatePath, true);
            File.Delete(ZipPath);

            return true;
        }

        private bool IsUpdateAvailable(string currentVersion, string newVersion)
        {
            var currentVersionSplit = currentVersion.Split('.');
            var newVersionSplit = newVersion.Split('.');

            for (int i = 0; i < currentVersionSplit.Length; i++)
            {
                var currentVersionPart = int.Parse(currentVersionSplit[i]);
                var newVersionPart = int.Parse(newVersionSplit[i]);

                if (newVersionPart > currentVersionPart)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task DownloadLatest()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var downloadUrl = "https://github.com/FCornaire/TF.EX/releases/latest/download/TF.EX.zip";

                var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);

                if (Directory.Exists(DownloadPath))
                {
                    Directory.Delete(DownloadPath, true);
                }
                Directory.CreateDirectory(DownloadPath);


                File.WriteAllBytes($"{DownloadPath}/update.zip", fileBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while trying to download latest version: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private string GetVersion(string jsonText)
        {
            string pattern = "\"version\": \"(.*?)\"";

            Match match = Regex.Match(jsonText, pattern);
            if (match.Success)
            {
                string version = match.Groups[1].Value;
                return version;
            }

            return string.Empty;
        }

        public bool IsUpdateAvailable()
        {
            return _isUpdateAvailable;
        }
    }
}
