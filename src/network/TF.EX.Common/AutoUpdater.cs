using MessagePack;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        string GetFailureReason();
        Task<bool> DownloadAndApply(Action<string> onPhase, Action<long, long> onProgress);
    }

    public partial class AutoUpdater(ILogger logger, string fortRisePath, string currentVersion, Func<string, bool> supportsFortRise) : IAutoUpdater
    {

        [GeneratedRegex(@"v\d+\.\d+\.\d+")]
        private static partial Regex VersionRegex();

        private const string ModName = "TF.EX";
        private const string BundleName = "DShad.TF.EX.zip";
        private const string BundleMeta = "DShad.TF.EX/meta.json";

        private static readonly string[] LegacyFolders = { "DShad.TF.EX", "DShad.TF.Replay", "DShad.TF.State", "DShad.TF.InputDisplayer" };

        //Stream.CopyToAsync's default
        private const int CopyBufferSize = 81920;

        private readonly ILogger _logger = logger;
        private readonly string _fortRisePath = fortRisePath;
        private readonly Func<string, bool> _supportsFortRise = supportsFortRise;
        private string DownloadPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "TF.EX", "Update");

        private string ZipPath => Path.Combine(DownloadPath, "update.zip");
        private string ModsPath => Path.Combine(_fortRisePath, "Mods");

        private string ModUpdaterPath => Path.Combine(_fortRisePath, "ModUpdater");

        private static readonly TimeSpan CheckTimestamp = TimeSpan.FromMinutes(5);

        private UpdateStatus _status = UpdateStatus.Unknown;
        private DateTime _lastSuccessfulCheck = DateTime.MinValue;
        private string _downloadUrl;

        private Version latestVersion;
        private readonly Version currentVersion = new(currentVersion);
        private string _failureReason;

        public async Task CheckForUpdate()
        {
            if (IsStatusFresh())
            {
                return;
            }

            try
            {
                CleanupPreviousUpdate();

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
                _status = UpdateStatus.Unknown;
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

        public string GetFailureReason()
        {
            return _failureReason;
        }

        public async Task<bool> DownloadAndApply(Action<string> onPhase, Action<long, long> onProgress)
        {
            _failureReason = null;

            try
            {
                onPhase?.Invoke($"DOWNLOADING V{latestVersion}");
                await Download(onProgress);

                onPhase?.Invoke("APPLYING UPDATE");
                var requiredFortRise = Validate();
                StageVersion(requiredFortRise);
                RemoveLegacyFolders();

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

        private string Validate()
        {
            using var zip = ZipFile.OpenRead(ZipPath);

            var entry = zip.GetEntry(BundleMeta) ?? throw new InvalidOperationException($"The downloaded archive has no {BundleMeta}");

            using var stream = entry.Open();
            using var meta = JsonDocument.Parse(stream);

            var version = new Version(meta.RootElement.GetProperty("version").GetString());

            if (version != latestVersion)
            {
                throw new InvalidOperationException($"The downloaded archive holds {version} instead of {latestVersion}");
            }

            var requiredFortRise = meta.RootElement.GetProperty("dependencies").EnumerateArray()
                .Where(dependency => dependency.GetProperty("name").GetString() == "FortRise")
                .Select(dependency => dependency.GetProperty("version").GetString())
                .FirstOrDefault();

            if (requiredFortRise != null && !_supportsFortRise(requiredFortRise))
            {
                _failureReason = $"FortRise {requiredFortRise} is required, update FortRise first";
                throw new InvalidOperationException(_failureReason);
            }

            return requiredFortRise;
        }

        private void StageVersion(string requiredFortRise)
        {
            Directory.CreateDirectory(ModUpdaterPath);

            var staged = Path.Combine(ModUpdaterPath, BundleName);
            File.Copy(ZipPath, staged, true);

            var listPath = Path.Combine(ModUpdaterPath, "updater.json");
            var entries = File.Exists(listPath) && JsonNode.Parse(File.ReadAllText(listPath)) is JsonArray existing
                ? existing
                : [];

            foreach (var previous in entries.Where(entry => entry?["ModName"]?.GetValue<string>() == ModName).ToList())
            {
                entries.Remove(previous);
            }

            entries.Add(new JsonObject
            {
                ["ModName"] = ModName,
                ["Version"] = currentVersion.ToString(),
                ["UpdateVersion"] = latestVersion.ToString(),
                ["FortRiseRequiredVersion"] = requiredFortRise ?? "0.0.0",
                ["ModPath"] = BundleName,
                ["UpdateModPath"] = staged,
                ["IsZipped"] = true,
            });

            File.WriteAllText(listPath, entries.ToJsonString());

            _logger.LogDebug<AutoUpdater>($"Staged {latestVersion} in {staged}");
        }

        private void RemoveLegacyFolders()
        {
            foreach (var folder in LegacyFolders)
            {
                var path = Path.Combine(ModsPath, folder);

                if (!Directory.Exists(path))
                {
                    continue;
                }

                File.Delete(Path.Combine(path, "meta.json"));

                try
                {
                    Directory.Delete(path, true);
                    _logger.LogDebug<AutoUpdater>($"Removed {folder}");
                }
                catch (Exception ex)
                {
                    _logger.LogError<AutoUpdater>($"Could remove {folder}", ex);
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

                foreach (var folder in LegacyFolders)
                {
                    var path = Path.Combine(ModsPath, folder);

                    if (Directory.Exists(path) && !File.Exists(Path.Combine(path, "meta.json")))
                    {
                        Directory.Delete(path, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError<AutoUpdater>("Could not clean the previous update", ex);
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

        private async Task<string> ResolveDownloadUrl(string tag)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Towerfall");
            var response = await client.GetAsync($"https://api.github.com/repos/fcornaire/tf.ex/releases/tags/{tag}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() == $"DShad.TF.EX-{tag}.zip")
                {
                    return asset.GetProperty("browser_download_url").GetString();
                }
            }

            return null;
        }
    }
}
