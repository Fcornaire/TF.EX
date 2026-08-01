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
        private static readonly string[] ModFolders = { "DShad.TF.EX", "DShad.TF.Replay", "DShad.TF.State" };

        private readonly ILogger _logger;
        private string DownloadPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TF.EX", "Update");

        private string ModsPath
        {
            get
            {
                var fortRise = Path.Combine(Directory.GetCurrentDirectory(), "FortRise", "Mods");

                return Directory.Exists(fortRise)
                    ? fortRise
                    : Path.Combine(Directory.GetCurrentDirectory(), "Mods");
            }
        }

        private string ZipPath => Path.Combine(DownloadPath, "update.zip");

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
                var meta = File.ReadAllText(Path.Combine(ModsPath, "DShad.TF.EX", "meta.json"));
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

                    if (!ExtractUpdate())
                    {
                        return;
                    }

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

        private bool ExtractUpdate()
        {
            try
            {
                if (!Directory.Exists(DownloadPath))
                {
                    _logger.LogError<AutoUpdater>("No update found");
                    return false;
                }

                if (!File.Exists(ZipPath))
                {
                    _logger.LogError<AutoUpdater>("No update found");
                    return false;
                }

                _logger.LogDebug<AutoUpdater>("Extracting update...");

                ZipFile.ExtractToDirectory(ZipPath, DownloadPath);

                if (!ModFolders.Any(folder => Directory.Exists(Path.Combine(DownloadPath, folder))))
                {
                    _logger.LogError<AutoUpdater>("The downloaded archive holds none of the mod folders");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>($"Exception while trying to extract update", ex);
                return false;
            }
        }

        public bool Update()
        {
            if (!_isUpdateAvailable)
            {
                _logger.LogError<AutoUpdater>("No TF.EX UPDATE AVAILABLE");
                return false;
            }

            foreach (var folder in ModFolders)
            {
                var source = Path.Combine(DownloadPath, folder);

                if (!Directory.Exists(source))
                {
                    _logger.LogDebug<AutoUpdater>($"{folder} is not part of the update, skipping");
                    continue;
                }

                MoveInto(source, Path.Combine(ModsPath, folder));

                Directory.Delete(source, true);
                _logger.LogDebug<AutoUpdater>($"Updated {folder}");
            }

            _logger.LogDebug<AutoUpdater>("Deleted Update files");
            File.Delete(ZipPath);
            _logger.LogDebug<AutoUpdater>("Deleted Update zip");

            _logger.LogDebug<AutoUpdater>("Update complete! Restarting TowerFall");

            return true;
        }

        private void MoveInto(string source, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(destinationFolder, Path.GetRelativePath(source, file));

                Directory.CreateDirectory(Path.GetDirectoryName(destination));

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                File.Move(file, destination);

                _logger.LogDebug<AutoUpdater>($"Updated {Path.GetFileName(file)}");
            }
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
                var downloadUrl = $"https://github.com/FCornaire/TF.EX/releases/download/v{latestVersion}/DShad.TF.EX-v{latestVersion}.zip";

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
                _logger.LogError<AutoUpdater>($"Exception while trying to download {latestVersion}", ex);
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
