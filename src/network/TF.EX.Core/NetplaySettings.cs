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
        public string Server { get; set; } = NetplayPreferences.OfficialServer;
        public string AutoAdjustInputDelay { get; set; } = "PROPOSE";
        public string CustomSkins { get; set; } = "FULL";
        public bool AutoUpdate { get; set; } = true;
        public string PlayerId { get; set; } = "";

        private static readonly string[] AutoAdjustModes = { "DISABLED", "PROPOSE", "ENABLED" };
        private static readonly string[] CustomSkinModes = { "DISABLED", "FULL" };

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
                "FRAMES OF LOCAL INPUT DELAY. RAISE IT TO SMOOTH OUT A LAGGY CONNECTION. \n DO NOTE THAT LESS INPUT DELAY PLAY BETTER BUT ROLLBACK MORE. \n MORE INPUT DELAY MEAN LESS ROLLBACK BUT PLAY CLUNCKY. \n FIND THE SWEETSPOT IN BETWEEN",
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
                "ADAPT THE INPUT DELAY TO THE CONNECTION WHEN JOINING A LOBBY. \n PROPOSE SUGGESTS A VALUE YOU CAN ACCEPT OR IGNORE. \n HOLD THE BUTTON TO RESET. \n ENABLED APPLIES IT AUTOMATICALLY. \n (NOTE: YOUR SAVED INPUT DELAY IS NEVER CHANGED)");

            settings.CreateOptions(
                "CUSTOM SKINS",
                CustomSkins,
                CustomSkinModes,
                selection =>
                {
                    CustomSkins = selection.Item1;
                    Apply();
                },
                "SHOW OPPONENTS' CUSTOM ARCHER SKINS. \n SKINS ARE VISUAL ONLY, STREAMED IN MEMORY AND NEVER SAVED. \n SOUNDS AND MUSIC ARE OMITTED AND RE USE VANILLA");

            settings.CreateInput(
                "NETPLAY NAME",
                Name,
                value =>
                {
                    Name = value;
                    Apply();
                },
                "YOUR PLAYER NAME, SHOWN TO OPPONENTS IN LOBBIES AND MATCHES");

            settings.CreateCustomOptions(() => new ServerOptionsButton(this,
                "THE MATCHMAKING SERVER TO CONNECT TO. \n ONLY CHANGE THIS IF YOU KNOW WHAT YOU ARE DOING"));

            settings.CreateOnOff(
                "AUTO UPDATE",
                AutoUpdate,
                value =>
                {
                    AutoUpdate = value;
                    Apply();
                },
                "DOWNLOAD AND APPLY THE LATEST EX VERSION AUTOMATICALLY. \n ONLINE PLAY ALWAYS REQUIRES THE LATEST VERSION");
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

            var server = (Server ?? "").Trim().TrimEnd('/');
            if (server.Length == 0)
            {
                server = NetplayPreferences.OfficialServer;
            }
            Server = server;

            var mode = Enum.TryParse<AutoAdjustInputDelayMode>(AutoAdjustInputDelay, true, out var parsed)
                ? parsed
                : AutoAdjustInputDelayMode.Propose;
            AutoAdjustInputDelay = mode.ToString().ToUpperInvariant();

            var skinMode = Enum.TryParse<CustomSkinMode>(CustomSkins, true, out var parsedSkin)
                ? parsedSkin
                : CustomSkinMode.Full;
            CustomSkins = skinMode.ToString().ToUpperInvariant();

            var playerId = (PlayerId ?? "").Trim();
            if (playerId.Length == 0)
            {
                playerId = $"local:{Guid.NewGuid()}";
            }
            PlayerId = playerId;

            NetplayPreferences.InputDelay = InputDelay;
            NetplayPreferences.Name = Name;
            NetplayPreferences.PlayerId = PlayerId;
            NetplayPreferences.Server = Server;
            NetplayPreferences.AutoAdjustInputDelay = mode;
            NetplayPreferences.CustomSkins = skinMode;
            NetplayPreferences.AutoUpdate = AutoUpdate;
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
