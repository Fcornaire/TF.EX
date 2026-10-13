using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain
{
    public static class MatchReport
    {
        private const long MaxLogBytes = 4 * 1024 * 1024;
        private const int MaxNameLength = 48;
        private static readonly TimeSpan CrashUploadDuration = TimeSpan.FromSeconds(10);
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

        private static string _group;
        private static bool _hasDesynced;

        public static void Begin(IEnumerable<Models.WebSocket.Player> players)
        {
            _hasDesynced = false;
            _group = players == null ? null : string.Join("-", players.Select(player => Sanitize(player.Name)).Order(StringComparer.Ordinal));
        }

        public static void MarkDesync()
        {
            _hasDesynced = true;
        }

        public static void OnReplayExported(string replayPath)
        {
            var group = TakeGroup();

            if (group == null)
            {
                return;
            }

            var kind = _hasDesynced ? "desync" : IsMatchUnfinished() ? "disconnect" : null;

            if (kind != null)
            {
                _ = Task.Run(() => Upload(group, kind, replayPath, null));
            }
        }

        public static void OnCrash(Exception exception)
        {
            try
            {
                var group = TakeGroup();

                if (group == null)
                {
                    return;
                }

                string replayPath = null;

                try
                {
                    replayPath = ServiceCollections.ResolveReplayService().Export();
                }
                catch (Exception)
                {
                }

                Upload(group, "crash", replayPath, exception.ToString()).Wait(CrashUploadDuration);
            }
            catch (Exception)
            {
            }
        }

        private static string TakeGroup()
        {
            var group = _group;
            _group = null;

            return group;
        }

        private static bool IsMatchUnfinished()
        {
            return TFGame.Instance?.Scene is Level level && level.Session.GetWinner() == -1;
        }

        private static async Task Upload(string group, string kind, string replayPath, string exceptionText)
        {
            var logger = ServiceCollections.ResolveLogger();

            try
            {
                var name = Sanitize(NetplayPreferences.Name);
                var zip = BuildZip(name, replayPath, exceptionText);
                var url = $"{ToHttpUrl(NetplayPreferences.Server)}/report?group={Uri.EscapeDataString(group)}&name={Uri.EscapeDataString(name)}&kind={kind}";

                using var content = new ByteArrayContent(zip);
                using var response = await Http.PostAsync(url, content);

                logger.LogInformation("Sent {kind} report ({size} KB), server answered {status}", kind, zip.Length / 1024, (int)response.StatusCode);
            }
            catch (Exception e)
            {
                logger.LogWarning("Could not send {kind} report: {error}", kind, e.Message);
            }
        }

        private static byte[] BuildZip(string name, string replayPath, string exceptionText)
        {
            using var memory = new MemoryStream();

            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            {
                if (replayPath != null && File.Exists(replayPath))
                {
                    archive.CreateEntryFromFile(replayPath, $"{name}_{Path.GetFileName(replayPath)}", CompressionLevel.SmallestSize);
                }

                var log = ReadCurrentLog();

                if (log != null)
                {
                    AddEntry(archive, $"{name}_log.txt", log);
                }

                if (exceptionText != null)
                {
                    AddEntry(archive, $"{name}_exception.txt", Encoding.UTF8.GetBytes(exceptionText));
                }
            }

            return memory.ToArray();
        }

        private static void AddEntry(ZipArchive archive, string entryName, byte[] bytes)
        {
            using var entry = archive.CreateEntry(entryName, CompressionLevel.SmallestSize).Open();
            entry.Write(bytes, 0, bytes.Length);
        }

        private static byte[] ReadCurrentLog()
        {
            var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(folder))
            {
                return null;
            }

            var started = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var files = new DirectoryInfo(folder).GetFiles("*.txt");
            var current = files.Where(file => file.CreationTimeUtc >= started).MinBy(file => file.CreationTimeUtc)
                ?? files.MaxBy(file => file.LastWriteTimeUtc);

            if (current == null)
            {
                return null;
            }

            using var stream = new FileStream(current.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var start = Math.Max(0, stream.Length - MaxLogBytes);
            var bytes = new byte[stream.Length - start];

            stream.Seek(start, SeekOrigin.Begin);
            stream.ReadExactly(bytes);

            return bytes;
        }

        private static string ToHttpUrl(string server)
        {
            if (server.StartsWith("wss://"))
            {
                return string.Concat("https://", server.AsSpan("wss://".Length));
            }

            if (server.StartsWith("ws://"))
            {
                return string.Concat("http://", server.AsSpan("ws://".Length));
            }

            return server;
        }

        private static string Sanitize(string value)
        {
            var sanitized = new string([.. value.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_').Take(MaxNameLength)]);

            return sanitized.Length > 0 ? sanitized : "PLAYER";
        }
    }
}
