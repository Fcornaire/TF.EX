using FortRise;
using Microsoft.Extensions.Logging;
using Monocle;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TowerFall;

namespace TF.EX.Core
{
    internal class TFCommands
    {
        private const int DEFAULT_SCENARIO_FRAMES = 1800;

        private const int DEFAULT_SCENARIO_SEED = 42;

        public Dictionary<string, CommandConfiguration> Commands = [];

        public TFCommands()
        {
            Initialize();
        }

        public Dictionary<string, CommandConfiguration> GetCommands()
        {
            return Commands;
        }

        private void Initialize()
        {
            Commands.Add("test", new CommandConfiguration
            {
                Callback = LaunchTestMode,
            });
            Commands.Add("scenario", new CommandConfiguration
            {
                Callback = LaunchScenario,
            });
            Commands.Add("local", new CommandConfiguration
            {
                Callback = LaunchLocalNetplay,
            });
            Commands.Add("replay", new CommandConfiguration
            {
                Callback = LaunchReplay,
            });
            Commands.Add("online", new CommandConfiguration
            {
                Callback = LaunchServerNetplay,
            });
            Commands.Add("menu", new CommandConfiguration
            {
                Callback = SwitchToMenu,
            });
            Commands.Add("vs", new CommandConfiguration
            {
                Callback = LaunchVersus,
            });
            Commands.Add("dump", new CommandConfiguration
            {
                Callback = DumpEntities,
            });
        }

        public void Register(IModuleContext context)
        {
            foreach (var command in Commands)
            {
                context.Registry.Commands.RegisterCommands(command.Key, command.Value);
            }
        }

        public static void LaunchTestMode(string[] args)
        {
            args = CollapseVariantBlock(args);

            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var map = ParseMap(args, 2);
            var startLevel = ParseStartLevel(args, 1, map);
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;
            var checkDistance = args.Length > 4 ? Math.Min(int.Parse(args[4]), 7) : 2;
            var variants = ParseVariants(args, 5);
            var playerCount = args.Length > 6 ? Math.Clamp(int.Parse(args[6]), 2, 4) : 2;

            StartTestMode(mode, map, startLevel, seed, checkDistance, variants, playerCount, null);
        }

        private static void StartTestMode(TowerFall.Modes mode, int map, int startLevel, int seed, int checkDistance, List<string> variants, int playerCount, List<string> levelPaths)
        {
            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching test mode with mode: {mode}, map: {map} ({GameData.VersusTowers[map].Theme.Name}), startLevel: {startLevel}, seed: {seed}, checkDistance: {checkDistance}, players: {playerCount} with variants [{string.Join(", ", variants)}]");

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch test mode while being in another netplay mode");
                return;
            }

            netplayManager.SetTestMode(checkDistance, playerCount);

            StateApi.Current.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < playerCount; i++)
            {
                TFGame.PlayerInputs[i] ??= new FakeController();
            }

            StartGame(mode, netplayManager, map, startLevel, variants, playerCount, levelPaths);

            TFGame.Instance.Commands.Open = false;
        }

        public static void LaunchScenario(string[] args)
        {
            args = CollapseVariantBlock(args);

            var names = ParseScenarioNames(args, 0);
            var framesPerRun = args.Length > 1 ? Math.Max(60, int.Parse(args[1])) : DEFAULT_SCENARIO_FRAMES;

            var seedArg = args.Length > 2 ? int.Parse(args[2]) : (int?)null;

            var startIndex = 0;

            if (args.Length > 0 && int.TryParse(args[0], out var parsedIndex))
            {
                startIndex = Math.Max(0, parsedIndex);
                names = [];
            }

            var scenarios = TF.EX.Domain.Scenarios.ScenarioCatalog.Resolve(names);

            if (startIndex > 0)
            {
                scenarios = [.. scenarios.Skip(startIndex)];
            }
            var logger = ServiceCollections.ResolveLogger();

            if (scenarios.Length == 0)
            {
                var known = TF.EX.Domain.Scenarios.ScenarioCatalog.All.Select((scenario, index) => $"{index}:{scenario.Name}");

                TFGame.Instance.Commands.Log($"No matching scenario. Known: {string.Join(", ", known)}");
                return;
            }

            if (ScenarioSweeper.IsRunning)
            {
                ScenarioSweeper.Abort("restarted");
            }

            ResetForNextRun();

            var runs = new List<(string, Action, int, Func<TowerFall.Level, bool>)>();

            for (int i = 0; i < scenarios.Length; i++)
            {
                var captured = scenarios[i];
                var index = startIndex + i;
                var path = TF.EX.Domain.Scenarios.ScenarioMapWriter.Write(captured);
                var runSeed = seedArg ?? captured.Seed ?? DEFAULT_SCENARIO_SEED;

                runs.Add(($"scenario {index:00} {captured.Name}", () =>
                {
                    InputScripter.Start(captured.PlayerCount, captured.Scripts);

                    StartTestMode(
                        captured.Mode,
                        0,
                        0,
                        runSeed,
                        2,
                        [.. captured.Variants],
                        captured.PlayerCount,
                        [path]);
                }
                , captured.Frames, captured.Expect));
            }

            logger.LogInformation($"[sweep] starting {runs.Count} scenarios from index {startIndex}, seed {(seedArg.HasValue ? seedArg.Value.ToString() : $"{DEFAULT_SCENARIO_SEED}")}");

            ScenarioSweeper.Start(runs, framesPerRun, ReturnToMenu, () =>
            {
                TF.EX.Domain.Scenarios.ScenarioCatalog.LogCoverage(line => logger.LogInformation(line));

                StateApi.Current.SetVersusLevels(null, null);

                ReturnToMenu();
            });

            TFGame.Instance.Commands.Open = false;
        }

        private static void ResetForNextRun()
        {
            var manager = ServiceCollections.ResolveNetplayManager();

            manager.Reset();
            manager.ResetMode();

            for (int seat = 0; seat < TFGame.Players.Length; seat++)
            {
                TFGame.Players[seat] = false;
            }

            for (int seat = 0; seat < TFGame.PlayerInputs.Length; seat++)
            {
                if (TFGame.PlayerInputs[seat] is FakeController)
                {
                    TFGame.PlayerInputs[seat] = null;
                }
            }
        }

        private static void ReturnToMenu()
        {
            ResetForNextRun();

            TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);
        }

        public static void LaunchLocalNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var remoteAddr = args.Length > 1 ? args[1] : "";
            var remotePort = args.Length > 2 ? int.Parse(args[2]) : 7001;
            var localPort = args.Length > 3 ? ushort.Parse(args[3]) : (ushort)7000;
            var map = ParseMap(args, 4);
            var startLevel = ParseStartLevel(args, 5, map);
            var draw = args.Length > 6 ? ParseDraw(args[6]) : PlayerDraw.Player1;
            var seed = args.Length > 7 ? int.Parse(args[7]) : 42;


            if (string.IsNullOrEmpty(remoteAddr))
            {
                TFGame.Instance.Commands.Log("You must specify an address in your local network");
                return;
            }

            if (IPAddress.TryParse(remoteAddr, out var ip))
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                {
                    TFGame.Instance.Commands.Log("You must specify an IPv4 address");
                    return;
                }
            }
            else
            {
                TFGame.Instance.Commands.Log("You must specify a valid IPv4 address");
                return;
            }

            remoteAddr = $"{remoteAddr}:{remotePort}";

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch local mode while being in another netplay mode");
                return;
            }

            netplayManager.SetLocalMode(remoteAddr, localPort, draw);

            StateApi.Current.SetSeed(seed);
            replayService.Initialize();

            StartGame(mode, netplayManager, map, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        public static void LaunchReplay(string[] args)
        {
            var replayName = args.Length > 0 ? args[0] : "";

            if (string.IsNullOrEmpty(replayName))
            {
                TFGame.Instance.Commands.Log("You must specify a replay name");
                return;
            }

            replayName = $"{replayName}.tow";

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            Music.Stop();
            Sounds.ui_mapZoom.Play();

            Task.Run(async () =>
            {
                TFGame.Instance.Commands.Open = false;

                TFGame.Instance.Scene.AddLoader("LOADING REPLAY...");

                var failure = await replayService.LoadAndStart(replayName);

                TFGame.Instance.Scene.RemoveLoader();

                if (failure != null)
                {
                    TFGame.Instance.Commands.Log(failure);
                }
            });
        }

        public static void LaunchServerNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var roomUrl = args.Length > 1 ? args[1] : "";
            var seed = args.Length > 2 ? int.Parse(args[2]) : 42;

            if (string.IsNullOrEmpty(roomUrl))
            {
                TFGame.Instance.Commands.Log("You must specify a room url (Server + roomId)");
                return;
            }

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();


            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch online mode while being in another netplay mode");
                return;
            }

            StateApi.Current.SetSeed(seed);
            netplayManager.SetServerMode(roomUrl);
            replayService.Initialize();

            StartGame(mode, netplayManager, 0);

            TFGame.Instance.Commands.Open = false;
        }

        public static void SwitchToMenu(string[] args)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            netplayManager.ResetMode();

            TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);

            TFGame.Instance.Commands.Open = false;
        }

        public static void LaunchVersus(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var map = ParseMap(args, 2);
            var startLevel = ParseStartLevel(args, 1, map);
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;

            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching versus mode with mode: {mode}, map: {map} ({GameData.VersusTowers[map].Theme.Name}), startLevel: {startLevel}, seed: {seed}");

            var replayService = ServiceCollections.ResolveReplayService();

            StateApi.Current.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            StartGame(mode, null, map, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        public static void DumpEntities(string[] args)
        {
            if (TFGame.Instance.Scene is not Level level)
            {
                TFGame.Instance.Commands.Log("dump: not currently in a Level");
                return;
            }

            var dump = TF.EX.Domain.Interop.StateApi.Current.DumpEntities();

            var fileName = (args.Length > 0 ? args[0] : "entity_dump") + ".txt";
            var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            File.WriteAllText(path, dump);

            TFGame.Instance.Commands.Log($"dump: wrote entity dump to {path}");
        }

        private static int ParseMap(string[] args, int index)
        {
            return args.Length > index
                ? Math.Clamp(int.Parse(args[index]), 0, GameData.VersusTowers.Count - 1)
                : 0;
        }

        private static int ParseStartLevel(string[] args, int index, int map)
        {
            return args.Length > index
                ? Math.Clamp(int.Parse(args[index]), 0, GameData.VersusTowers[map].Levels.Count - 1)
                : 0;
        }

        private static TowerFall.Modes ParseMode(string arg)
        {
            switch (arg.ToUpper())
            {
                case "LMS": return TowerFall.Modes.LastManStanding;
                case "HH": return TowerFall.Modes.HeadHunters;
                case "TDM": return TowerFall.Modes.TeamDeathmatch;
                default: return TowerFall.Modes.LastManStanding;
            }
        }

        private static PlayerDraw ParseDraw(string arg)
        {
            switch (arg)
            {
                case "P1": return PlayerDraw.Player1;
                case "P2": return PlayerDraw.Player2;
                default: return PlayerDraw.Player1;
            }
        }

        private static string[] CollapseVariantBlock(string[] args)
        {
            if (!args.Any(a => a.StartsWith("#")))
            {
                return args;
            }

            var collapsed = new List<string>();
            List<string> block = null;

            foreach (var arg in args)
            {
                if (block == null && !arg.StartsWith("#"))
                {
                    collapsed.Add(arg);
                    continue;
                }

                block ??= [];
                block.Add(arg);

                if (block.Count > 0 && arg.EndsWith("#") && !(block.Count == 1 && arg == "#"))
                {
                    collapsed.Add(string.Join(" ", block));
                    block = null;
                }
            }

            if (block != null)
            {
                collapsed.Add(string.Join(" ", block));
            }

            return [.. collapsed];
        }

        private static List<string> ParseScenarioNames(string[] args, int index)
        {
            if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
            {
                return [];
            }

            var raw = args[index].Trim();

            if (raw.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return [];
            }

            return [.. raw.Trim('#')
                .Split(',')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrEmpty(n))];
        }

        private static List<string> ParseVariants(string[] args, int index)
        {
            if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
            {
                return [];
            }

            return [.. args[index].Trim().Trim('#')
                .Split(',')
                .Select(v => v.Trim().Replace("_", " ").ToUpper(CultureInfo.InvariantCulture))
                .Where(v => !string.IsNullOrEmpty(v))];
        }

        private static void WarnOnUnknownVariants(MatchVariants matchVariants, IEnumerable<string> variants)
        {
            foreach (var variant in variants)
            {
                var known = matchVariants.Variants.Any(v => v.Title == variant) || matchVariants.CustomVariants.Any(v => v.Value.Title == variant);

                if (!known)
                {
                    ServiceCollections.ResolveLogger().LogWarning($"[sweep] unknown variant '{variant}'");
                }
            }
        }

        private static void StartGame(TowerFall.Modes mode, INetplayManager netplayManager = null, int map = 0, int startLevel = 0, List<string> variants = null, int playerCount = 0, List<string> levelPaths = null)
        {
            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = playerCount > 0 ? i < playerCount : TFGame.PlayerInputs[i] != null;
            }

            MatchSettings matchSettings = MainMenu.VersusMatchSettings ?? MatchSettings.GetDefaultVersus();
            matchSettings.LevelSystem = GameData.VersusTowers[map].GetLevelSystem();
            matchSettings.Mode = mode;

            MainMenu.CurrentMatchSettings = matchSettings;

            if (matchSettings.TeamMode)
            {
                for (int i = 0; i < 4; i++)
                {
                    matchSettings.Teams[i] = i % 2 == 0 ? Allegiance.Blue : Allegiance.Red;
                }
            }

            variants ??= [];
            WarnOnUnknownVariants(matchSettings.Variants, variants);
            matchSettings.Variants.ApplyVariants(variants);

            TF.EX.Domain.Interop.StateApi.Current.GenerateVersusLevels(matchSettings, map, startLevel);
            TF.EX.Domain.Interop.StateApi.Current.SetVersusLevels(matchSettings, levelPaths);

            var session = new Session(matchSettings);

            if (netplayManager != null)
            {
                netplayManager.Init(session.RoundLogic);
            }

            session.StartGame();
        }
    }
}
