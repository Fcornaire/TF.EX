using MessagePack;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TF.EX.Common.Extensions;

namespace TF.EX.Common
{
    [DataContract]
    public class GithubTag
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    public interface IAutoUpdater
    {
        Task CheckForUpdate();
        bool IsUpdateAvailable();
        bool Update();
        Version GetLatestVersion();
    }

    public class AutoUpdater : IAutoUpdater
    {
        private readonly ILogger _logger;
        private string DownloadPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TF.EX", "Update");
        private string UpdatePath => $"{DownloadPath}/TF.EX";

        private string ZipPath => $"{DownloadPath}/update.zip";

        private bool _isUpdateAvailable = false;

        private Version latestVersion;
        private Version currentVersion;

        public AutoUpdater(ILogger logger)
        {
            _logger = logger;
        }

        public async Task CheckForUpdate()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                var meta = File.ReadAllText(".\\Mods\\TF.EX\\meta.json");
                currentVersion = GetVersion(meta);

                _logger.LogDebug<AutoUpdater>($"Current TF.EX version: {currentVersion}");
                _logger.LogDebug<AutoUpdater>($"Checking latest TF.EX version");

                latestVersion = await GetLatestVersion();

                _logger.LogDebug<AutoUpdater>($"Latest TF.EX version: {latestVersion}");

                if (latestVersion > currentVersion)
                {
                    var hasRelease = await HasARelease($"v{latestVersion}");

                    if (!hasRelease)
                    {
                        _logger.LogDebug<AutoUpdater>("No TF.EX Update available");
                        return;
                    }

                    _logger.LogDebug<AutoUpdater>("TF.EX Update available!");
                    await DownloadLatest();
                    ExtractUpdate();
                    _logger.LogDebug<AutoUpdater>($"Donwloaded and extracted Update {latestVersion}");

                    _isUpdateAvailable = true;
                }
                else
                {
                    _logger.LogDebug<AutoUpdater>("No TF.EX Update available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>($"Error while trying to check for Update", ex);
            }
        }

        private void ExtractUpdate()
        {
            try
            {
                if (!Directory.Exists(DownloadPath))
                {
                    _logger.LogError<AutoUpdater>("No update found");
                    return;
                }

                if (!File.Exists(ZipPath))
                {
                    _logger.LogError<AutoUpdater>("No update found");
                    return;
                }

                _logger.LogDebug<AutoUpdater>("Extracting update...");

                ZipFile.ExtractToDirectory(ZipPath, DownloadPath);
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>($"Exception while trying to extract update", ex);
            }
        }

        public bool Update()
        {
            if (!_isUpdateAvailable)
            {
                _logger.LogError<AutoUpdater>("No TF.EX UPDATE AVAILABLE");
                return false;
            }

            var updateFiles = Directory.GetFiles(UpdatePath, "*", SearchOption.AllDirectories);

            foreach (var file in updateFiles)
            {
                var fileName = Path.GetFileName(file);
                var destination = $"{Directory.GetCurrentDirectory()}\\{file.Replace(UpdatePath, "Mods\\TF.EX")}";

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(file, destination);

                _logger.LogDebug<AutoUpdater>($"Updated {fileName}");
            }

            Directory.Delete(UpdatePath, true);
            _logger.LogDebug<AutoUpdater>("Deleted Update files");
            File.Delete(ZipPath);
            _logger.LogDebug<AutoUpdater>("Deleted Update zip");

            _logger.LogDebug<AutoUpdater>("Update complete! Restarting TowerFall");

            return true;
        }

        private async Task<Version> GetLatestVersion()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");
            var response = await client.GetAsync("https://api.github.com/repos/fcornaire/tf.ex/tags");
            var content = await response.Content.ReadAsStringAsync();
            var bytes = MessagePackSerializer.ConvertFromJson(content);
            var tags = MessagePackSerializer.Deserialize<List<GithubTag>>(bytes);

            var regex = new Regex(@"v\d+\.\d+\.\d+");
            var semverTags = tags.Select(t => t.Name).Where(tag => regex.IsMatch(tag)).ToList();
            var latestSemverTag = semverTags.OrderByDescending(t => new Version(t.Substring(1))).FirstOrDefault();

            return new Version(latestSemverTag.Substring(1));
        }

        private async Task<bool> HasARelease(string tag)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");
            var response = await client.GetAsync($"https://api.github.com/repos/fcornaire/tf.ex/releases/tags/{tag}");
            return response.IsSuccessStatusCode;
        }


        private async Task DownloadLatest()
        {
            try
            {
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
                _logger.LogError<AutoUpdater>($"Exception while trying to download latest version", ex);
            }
        }

        private Version GetVersion(string jsonText)
        {
            string pattern = "\"version\": \"(.*?)\"";

            Match match = Regex.Match(jsonText, pattern);
            if (match.Success)
            {
                string version = match.Groups[1].Value;
                return new Version(version);
            }

            throw new InvalidOperationException("Unable to get version from meta.json");
        }

        public bool IsUpdateAvailable()
        {
            return _isUpdateAvailable;
        }

        Version IAutoUpdater.GetLatestVersion()
        {
            return latestVersion;
        }
    }
}
