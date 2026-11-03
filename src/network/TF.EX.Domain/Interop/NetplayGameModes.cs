using Microsoft.Extensions.Logging;
using Monocle;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.Interop
{
    public class NetplayGameMode
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string ModName { get; init; }
        public bool IsTeamMode { get; init; }
        public int MinimumPlayers { get; init; }
        public Modes BaseMode { get; init; }
        public Func<Session, TowerFall.RoundLogic> CreateRoundLogic { get; init; }
        public Action<TowerFall.RoundLogic> SpawnPlayers { get; init; }

        public bool IsBuiltIn => ModName == null;

        public Subtexture Icon => IsBuiltIn || !FortRise.GameModeRegistry.RegistryVersusGameModes.TryGetValue(Id, out var entry)
            ? VersusModeButton.GetModeIcon(BaseMode)
            : entry.VersusGameMode.Icon.Subtexture;
    }

    public static class NetplayGameModes
    {
        public const string LastManStanding = "LastManStanding";
        public const string HeadHunters = "HeadHunters";
        public const string TeamDeathmatch = "TeamDeathmatch";

        private static readonly List<NetplayGameMode> modes =
        [
            BuiltIn(LastManStanding, Modes.LastManStanding),
            BuiltIn(HeadHunters, Modes.HeadHunters),
            BuiltIn(TeamDeathmatch, Modes.TeamDeathmatch),
        ];

        private static string testModeId;

        public static IReadOnlyList<NetplayGameMode> All => modes;

        private static NetplayGameMode BuiltIn(string id, Modes mode) => new()
        {
            Id = id,
            Name = VersusModeButton.GetModeName(mode),
            IsTeamMode = mode == Modes.TeamDeathmatch,
            MinimumPlayers = mode == Modes.TeamDeathmatch ? CustomComponent.LobbyVersusModeButton.TEAM_MODE_MIN_PLAYERS : 2,
            BaseMode = mode,
        };

        public static NetplayGameMode Register(string modName, string name, bool isTeamMode, int minimumPlayers, Func<Session, TowerFall.RoundLogic> createRoundLogic, Action<TowerFall.RoundLogic> spawnPlayers)
        {
            var mode = new NetplayGameMode
            {
                Id = $"{modName}/{name}",
                Name = name,
                ModName = modName,
                IsTeamMode = isTeamMode,
                MinimumPlayers = Math.Max(2, minimumPlayers),
                BaseMode = isTeamMode ? Modes.TeamDeathmatch : Modes.LastManStanding,
                CreateRoundLogic = createRoundLogic,
                SpawnPlayers = spawnPlayers,
            };

            modes.RemoveAll(registered => registered.Id == mode.Id);
            modes.Add(mode);

            return mode;
        }

        public static void Unregister(string modName, string name)
        {
            modes.RemoveAll(mode => mode.Id == $"{modName}/{name}");
        }

        public static NetplayGameMode Resolve(string id)
        {
            return modes.FirstOrDefault(mode => mode.Id == id);
        }

        public static string IdOf(Modes mode)
        {
            return modes.FirstOrDefault(registered => registered.IsBuiltIn && registered.BaseMode == mode)?.Id ?? LastManStanding;
        }

        public static string ModNameOf(string id)
        {
            var slash = string.IsNullOrEmpty(id) ? -1 : id.IndexOf('/');

            return slash > 0 ? id.Substring(0, slash) : null;
        }

        public static void SetTestMode(string id)
        {
            testModeId = id;
        }

        public static NetplayGameMode Selected()
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsReplayMode())
            {
                return Resolve(ReplayApi.Current?.GetLoadedReplayGameMode()) ?? Resolve(IdOf((Modes)(ReplayApi.Current?.GetLoadedReplayMode() ?? 0)));
            }

            if (netplayManager.IsTestMode() && testModeId != null)
            {
                return Resolve(testModeId);
            }

            var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();

            return Resolve(lobby.IsEmpty ? LastManStanding : lobby.GameData.Mode) ?? Resolve(LastManStanding);
        }

        public static string MissingMode(Lobby lobby)
        {
            var modName = ModNameOf(lobby.GameData.Mode);

            if (modName == null)
            {
                return null;
            }

            var hostMod = lobby.Mods.FirstOrDefault(mod => mod.Name == modName);
            var host = hostMod?.Data != null && hostMod.Data.TryGetValue(CustomMod.VersionKey, out var version) ? version : "";
            var installed = Resolve(lobby.GameData.Mode) != null ? ServiceCollections.ResolveModCollections()?.GetVersion(modName) : null;

            return installed != null && (host == "" || installed == host) ? null : $"{modName} {host}".Trim().ToUpperInvariant();
        }

        public static bool TrySpawnRoundPlayers(TowerFall.RoundLogic roundLogic, bool withVersusStart)
        {
            var selected = Selected();

            if (selected?.SpawnPlayers == null || roundLogic == null || roundLogic is IRoundPlayerSpawner || !roundLogic.Session.MatchSettings.Mode.IsNetplay())
            {
                return false;
            }

            if (withVersusStart)
            {
                roundLogic.Session.CurrentLevel.Add(new VersusStart(roundLogic.Session));
            }

            selected.SpawnPlayers(roundLogic);

            return true;
        }

        public static void OnLevelLoadFinish(TowerFall.RoundLogic roundLogic, Action spawnRoundPlayers)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            if (netplayManager.IsReplayMode())
            {
                if (roundLogic.Session.RoundIndex > 0)
                {
                    spawnRoundPlayers();
                }

                return;
            }
            if (netplayManager.HasSessionDisconnected())
            {
                return;
            }

            if (!netplayManager.IsInit())
            {
                ServiceCollections.ResolveLogger().LogInformation("Configuring and initializing current netplay session");
                netplayManager.Init(roundLogic);

                var lobby = ServiceCollections.ResolveMatchmakingService().GetOwnLobby();
                ServiceCollections.ResolveReplayService().Initialize(lobby.GameData, lobby.Mods);
                if (lobby.IsReportEnabled)
                {
                    MatchReport.Begin(netplayManager.IsServerMode() ? lobby.SessionPlayers : null);
                }

                TowerFall.TFGame.ConsoleEnabled = false;
            }
            else
            {
                spawnRoundPlayers();
            }
        }
    }
}
