using MessagePack;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
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

    public enum UpdateStatus
    {
        Unknown,
        UpToDate,
        UpdateAvailable,
    }

    public interface IAutoUpdater
    {
        Task CheckForUpdate();
        bool IsStatusFresh();
        UpdateStatus GetStatus();
        Version GetLatestVersion();
        Version GetCurrentVersion();
        Task<bool> DownloadAndApply(Action<string> onPhase, Action<long, long> onProgress);
    }

    public partial class AutoUpdater : IAutoUpdater
    {

        [GeneratedRegex(@"v\d+\.\d+\.\d+")]
        private static partial Regex VersionRegex();

        private static readonly string[] ModFolders = { "DShad.TF.EX", "DShad.TF.Replay", "DShad.TF.State", "DShad.TF.InputDisplayer" };

        //Stream.CopyToAsync's default (dotnet/runtime Stream.cs): largest 4096 multiple under the 85K large-object-heap threshold
        private const int CopyBufferSize = 81920;

        private readonly ILogger _logger;
        private string DownloadPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TF.EX", "Update");

        private string ZipPath => Path.Combine(DownloadPath, "update.zip");

        private static readonly TimeSpan CheckTimestamp = TimeSpan.FromMinutes(5);

        private UpdateStatus _status = UpdateStatus.Unknown;
        private DateTime _lastSuccessfulCheck = DateTime.MinValue;
        private string _downloadUrl;

        private Version latestVersion;
        private Version currentVersion;

        public AutoUpdater(ILogger logger)
        {
            _logger = logger;
        }

        private string GetModsPath()
        {
            var fortRise = Path.Combine(Directory.GetCurrentDirectory(), "FortRise", "Mods");

            return Directory.Exists(fortRise) ? fortRise : Path.Combine(Directory.GetCurrentDirectory(), "Mods");
        }

        public async Task CheckForUpdate()
        {
            if (IsStatusFresh())
            {
                return;
            }

            try
            {
                CleanupPreviousUpdate();

                var meta = File.ReadAllText(Path.Combine(GetModsPath(), "DShad.TF.EX", "meta.json"));
                currentVersion = GetVersion(meta);

                _logger.LogDebug<AutoUpdater>($"Current TF.EX version: {currentVersion}");
                _logger.LogDebug<AutoUpdater>($"Checking latest TF.EX version");

                latestVersion = await FetchLatestVersion();

                _logger.LogDebug<AutoUpdater>($"Latest TF.EX version: {latestVersion}");

                _downloadUrl = latestVersion > currentVersion ? await ResolveDownloadUrl($"v{latestVersion}") : null;

                if (_downloadUrl != null)
                {
                    _logger.LogDebug<AutoUpdater>($"TF.EX Update available! ({_downloadUrl})");
                    _status = UpdateStatus.UpdateAvailable;
                }
                else
                {
                    _logger.LogDebug<AutoUpdater>("No TF.EX Update available");
                    _status = UpdateStatus.UpToDate;
                }

                _lastSuccessfulCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>($"Error while trying to check for Update", ex);
            }
        }

        public bool IsStatusFresh()
        {
            return _status != UpdateStatus.Unknown && DateTime.UtcNow - _lastSuccessfulCheck < CheckTimestamp;
        }

        public UpdateStatus GetStatus()
        {
            return _status;
        }

        public Version GetLatestVersion()
        {
            return latestVersion;
        }

        public Version GetCurrentVersion()
        {
            return currentVersion;
        }

        public async Task<bool> DownloadAndApply(Action<string> onPhase, Action<long, long> onProgress)
        {
            try
            {
                onPhase?.Invoke($"DOWNLOADING V{latestVersion}");
                await Download(onProgress);

                onPhase?.Invoke("APPLYING UPDATE");
                Extract();
                Apply();

                Directory.Delete(DownloadPath, true);

                _logger.LogDebug<AutoUpdater>($"Update {latestVersion} applied, awaiting restart");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>($"Exception while trying to apply update {latestVersion}", ex);
                return false;
            }
        }

        private async Task Download(Action<long, long> onProgress)
        {
            var downloadUrl = _downloadUrl;

            if (Directory.Exists(DownloadPath))
            {
                Directory.Delete(DownloadPath, true);
            }
            
            Directory.CreateDirectory(DownloadPath);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1;

            using var source = await response.Content.ReadAsStreamAsync();
            using var destination = File.Create(ZipPath);

            var buffer = new byte[CopyBufferSize];
            long copied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read);
                copied += read;
                onProgress?.Invoke(copied, total);
            }
        }

        private void Extract()
        {
            _logger.LogDebug<AutoUpdater>("Extracting update...");

            ZipFile.ExtractToDirectory(ZipPath, DownloadPath);

            if (!ModFolders.Any(folder => Directory.Exists(Path.Combine(DownloadPath, folder))))
            {
                throw new InvalidOperationException("The downloaded archive holds none of the mod folders ?");
            }
        }

        private void Apply()
        {
            foreach (var folder in ModFolders)
            {
                var source = Path.Combine(DownloadPath, folder);

                if (!Directory.Exists(source))
                {
                    _logger.LogDebug<AutoUpdater>($"{folder} is not part of the update, skipping");
                    continue;
                }

                var destination = Path.Combine(GetModsPath(), folder);

                ClearFolder(destination);
                MoveInto(source, destination);

                _logger.LogDebug<AutoUpdater>($"Updated {folder}");
            }
        }

        //a native (ggrs_ffi.dll for example) cannot be deleted, it will be removed by CleanupPreviousUpdate after the restart
        private void ClearFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    File.Move(file, $"{file}.{Guid.NewGuid():N}.old", true);
                }
            }
        }

        private void CleanupPreviousUpdate()
        {
            try
            {
                if (Directory.Exists(DownloadPath))
                {
                    Directory.Delete(DownloadPath, true);
                }

                foreach (var folder in ModFolders)
                {
                    var destination = Path.Combine(GetModsPath(), folder);

                    if (!Directory.Exists(destination))
                    {
                        continue;
                    }

                    foreach (var file in Directory.GetFiles(destination, "*.old", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>("Could not clean the previous update", ex);
            }
        }

        private void MoveInto(string source, string destinationFolder)
        {
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(destinationFolder, Path.GetRelativePath(source, file));

                Directory.CreateDirectory(Path.GetDirectoryName(destination));

                File.Move(file, destination, true);
            }
        }

        private async Task<Version> FetchLatestVersion()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");
            var response = await client.GetAsync("https://api.github.com/repos/fcornaire/tf.ex/tags");
            var content = await response.Content.ReadAsStringAsync();
            var bytes = MessagePackSerializer.ConvertFromJson(content);
            var tags = MessagePackSerializer.Deserialize<List<GithubTag>>(bytes);

            var regex = VersionRegex();
            var semverTags = tags.Select(t => t.Name).Where(tag => regex.IsMatch(tag)).ToList();
            var latestSemverTag = semverTags.OrderByDescending(t => new Version(t.Substring(1))).FirstOrDefault();

            return new Version(latestSemverTag.Substring(1));
        }

        //a release without the bundle asset (pre-1.0 naming) counts as no update
        private async Task<string> ResolveDownloadUrl(string tag)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");
            var response = await client.GetAsync($"https://api.github.com/repos/fcornaire/tf.ex/releases/tags/{tag}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() == $"DShad.TF.EX-{tag}.zip")
                {
                    return asset.GetProperty("browser_download_url").GetString();
                }
            }

            return null;
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
    }
}
