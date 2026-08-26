using FortRise;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using TF.EX.Domain;
using TF.EX.Domain.Models;

namespace TF.EX
{
    public class NetplaySettings : ModuleSettings
    {
        public int InputDelay { get; set; } = 2;
        public string Name { get; set; } = "PLAYER";
        public string AutoAdjustInputDelay { get; set; } = "PROPOSE";

        private static readonly string[] AutoAdjustModes = { "DISABLED", "PROPOSE", "ENABLED" };

        public override void Create(ISettingsCreate settings)
        {
            settings.CreateNumber(
                "INPUT DELAY",
                InputDelay,
                value =>
                {
                    InputDelay = value;
                    Apply();
                },
                "Frames of local input delay. Raise it to smooth out a laggy connection. \n Do note that less input delay play better but rollback more. \n More input delay mean less rollback but play cluncky. \n Find the sweetspot in between",
                NetplayPreferences.MinInputDelay,
                NetplayPreferences.MaxInputDelay);

            settings.CreateOptions(
                "AUTO ADJUST INPUT DELAY",
                AutoAdjustInputDelay,
                AutoAdjustModes,
                selection =>
                {
                    AutoAdjustInputDelay = selection.Item1;
                    Apply();
                },
                "Adapt the input delay to the connection when joining a lobby. \n PROPOSE suggests a value you can accept or ignore. \n Hold the button to reset. \n ENABLED applies it automatically. \n (Note: Your saved input delay is never changed)");

            settings.CreateInput(
                "NETPLAY NAME",
                Name,
                value =>
                {
                    Name = value;
                    Apply();
                },
                "Your player name, shown to opponents in lobbies and matches");
        }

        public override void OnVerify()
        {
            MigrateLegacyConfig();
            Apply();
        }

        internal void Apply()
        {
            InputDelay = Math.Min(NetplayPreferences.MaxInputDelay, Math.Max(NetplayPreferences.MinInputDelay, InputDelay));

            var name = (Name ?? "").Trim().ToUpperInvariant();
            if (name.Length == 0)
            {
                name = "PLAYER";
            }
            Name = name.Substring(0, Math.Min(name.Length, NetplayPreferences.MaxNameLength));

            var mode = Enum.TryParse<AutoAdjustInputDelayMode>(AutoAdjustInputDelay, true, out var parsed)
                ? parsed
                : AutoAdjustInputDelayMode.Propose;
            AutoAdjustInputDelay = mode.ToString().ToUpperInvariant();

            NetplayPreferences.InputDelay = InputDelay;
            NetplayPreferences.Name = Name;
            NetplayPreferences.AutoAdjustInputDelay = mode;
        }

        private void MigrateLegacyConfig()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "netplay_meta.json");
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", "TF.EX", "TF.EX.settings.json");
                if (File.Exists(settingsPath) && File.ReadAllText(settingsPath).Contains("\"InputDelay\""))
                {
                    File.Delete(path);
                    return;
                }

                using var document = JsonDocument.Parse(File.ReadAllText(path));

                if (document.RootElement.TryGetProperty("InputDelay", out var inputDelay))
                {
                    InputDelay = inputDelay.GetInt32();
                }

                if (document.RootElement.TryGetProperty("Name", out var name))
                {
                    Name = name.GetString();
                }
            }
            catch (Exception e)
            {
                ServiceCollections.ResolveLogger()?.LogError(e, "Could not migrate the legacy netplay config");
            }
        }
    }
}
