using FortRise;
using HarmonyLib;
using Monocle;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TF.EX.Common.Extensions;
using TF.EX.Domain;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.TowerFallExtensions.Scene;
using TowerFall;

namespace TF.EX.Core
{
    internal class TFCommands
    {
        public Dictionary<string, CommandConfiguration> Commands = new();

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
        }

        public void Register(IModuleContext context)
        {
            foreach (var command in Commands)
            {
                context.Registry.Commands.RegisterCommands(command.Key, command.Value);
            }
        }

        //[Command("test")]
        public static void LaunchTestMode(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var startLevel = args.Length > 1 ? Math.Min(int.Parse(args[1]), 9) : 0;
            var map = args.Length > 2 ? Math.Min(int.Parse(args[2]), 14) : 0;
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;
            var checkDistance = args.Length > 4 ? Math.Min(int.Parse(args[4]), 7) : 2;
            var variant = args.Length > 5 ? args[5] : "";

            variant = variant.Replace("_", " ");

            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching test mode with mode: {mode}, startLevel: {startLevel}, seed: {seed}, checkDistance: {checkDistance} with variant {variant}");

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch test mode while being in another netplay mode");
                return;
            }

            netplayManager.SetTestMode(checkDistance);

            rngService.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            StartGame(mode, netplayManager, map, startLevel, variant);

            TFGame.Instance.Commands.Open = false;
        }

        //[Command("local")]
        public static void LaunchLocalNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var remoteAddr = args.Length > 1 ? args[1] : "";
            var remotePort = args.Length > 2 ? int.Parse(args[2]) : 7001;
            var localPort = args.Length > 3 ? ushort.Parse(args[3]) : (ushort)7000;
            var map = args.Length > 4 ? Math.Min(int.Parse(args[4]), 14) : 0;
            var startLevel = args.Length > 5 ? Math.Min(int.Parse(args[5]), 9) : 0;
            var draw = args.Length > 6 ? ParseDraw(args[6]) : PlayerDraw.Player1;
            var seed = args.Length > 7 ? int.Parse(args[7]) : 42;


            if (string.IsNullOrEmpty(remoteAddr))
            {
                TFGame.Instance.Commands.Log("You must specify an address in your local network (assuming the port is available)");
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
            var rngService = ServiceCollections.ResolveRngService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch local mode while being in another netplay mode");
                return;
            }

            netplayManager.SetLocalMode(remoteAddr, localPort, draw);

            rngService.SetSeed(seed);
            replayService.Initialize();

            StartGame(mode, netplayManager, map, startLevel);

            TFGame.Instance.Commands.Open = false;
        }

        //[Command("replay")]
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

                await replayService.LoadAndStart(replayName);

                netplayManager.SetReplayMode();

                TFGame.Instance.Scene.RemoveLoader();
            });
        }

        //[Command("online")]
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
            var rngService = ServiceCollections.ResolveRngService();


            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch online mode while being in another netplay mode");
                return;
            }

            rngService.SetSeed(seed);
            netplayManager.SetServerMode(roomUrl);
            replayService.Initialize();

            StartGame(mode, netplayManager, 0);

            TFGame.Instance.Commands.Open = false;
        }

        //[Command("menu")]
        public static void SwitchToMenu(string[] args)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            netplayManager.ResetMode();

            TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);

            TFGame.Instance.Commands.Open = false;
        }

        //[Command("vs")]
        public static void LaunchVersus(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var startLevel = args.Length > 1 ? Math.Min(int.Parse(args[1]), 9) : 0;
            var map = args.Length > 2 ? Math.Min(int.Parse(args[2]), 14) : 0;
            var seed = args.Length > 3 ? int.Parse(args[3]) : 42;

            var logger = ServiceCollections.ResolveLogger();
            logger.LogDebug<Commands>($"Launching versus mode with mode: {mode}, startLevel: {startLevel}, seed: {seed}");

            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            rngService.SetSeed(seed);
            replayService.Initialize();

            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            StartGame(mode, null, map, startLevel);

            TFGame.Instance.Commands.Open = false;
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

        private static void StartGame(TowerFall.Modes mode, INetplayManager netplayManager = null, int map = 0, int startLevel = 0, string variant = "")
        {
            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            MatchSettings matchSettings = MatchSettings.GetDefaultVersus();
            matchSettings.LevelSystem = GameData.VersusTowers[map].GetLevelSystem();
            matchSettings.Mode = mode;
            if (!string.IsNullOrEmpty(variant))
            {
                matchSettings.Variants.ApplyVariants(new List<string> { variant });
            }

            var levels = (matchSettings.LevelSystem as VersusLevelSystem).OwnGenLevel(matchSettings, GameData.VersusTowers[map], null, ServiceCollections.ResolveRngService());
            Traverse.Create(matchSettings.LevelSystem).Field("levels").SetValue(levels);
            (matchSettings.LevelSystem as VersusLevelSystem).StartOnLevel(startLevel);

            var session = new Session(matchSettings);

            if (netplayManager != null)
            {
                netplayManager.Init(session.RoundLogic);
            }

            session.StartGame();
        }
    }
}
