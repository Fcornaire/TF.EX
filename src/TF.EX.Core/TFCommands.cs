using FortRise;
using System.Net;
using System.Net.Sockets;
using TF.EX.Domain;
using TF.EX.Domain.Models;
using TF.EX.Domain.Ports;
using TF.EX.Patchs.Engine;
using TowerFall;

namespace TF.EX.Core
{
    internal static class TFCommands
    {
        [Command("test")]
        public static void LaunchTestMode(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var startLevel = args.Length > 1 ? Math.Min(int.Parse(args[1]), 9) : 0;
            var seed = args.Length > 2 ? int.Parse(args[2]) : 42;
            var checkDistance = args.Length > 3 ? Math.Min(int.Parse(args[3]), 7) : 2;

            Console.WriteLine($"Launching test mode with mode: {mode}, startLevel: {startLevel}, seed: {seed}, checkDistance: {checkDistance}");

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

            StartGame(mode, startLevel, netplayManager);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("local")]
        public static void LaunchLocalNetplay(string[] args)
        {
            var mode = args.Length > 0 ? ParseMode(args[0]) : TowerFall.Modes.LastManStanding;
            var addr = args.Length > 1 ? args[1] : "";
            var startLevel = args.Length > 2 ? Math.Min(int.Parse(args[2]), 9) : 0;
            var draw = args.Length > 3 ? ParseDraw(args[3]) : PlayerDraw.Player1;
            var seed = args.Length > 4 ? int.Parse(args[4]) : 42;


            if (string.IsNullOrEmpty(addr))
            {
                TFGame.Instance.Commands.Log("You must specify an address in your local network (assuming the local port 7000 is available)");
                return;
            }

            if (IPAddress.TryParse(addr, out var ip))
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

            addr = $"{addr}:7000";

            var netplayManager = ServiceCollections.ResolveNetplayManager();
            var replayService = ServiceCollections.ResolveReplayService();
            var rngService = ServiceCollections.ResolveRngService();

            if (netplayManager.HasSetMode())
            {
                TFGame.Instance.Commands.Log("You can't launch local mode while being in another netplay mode");
                return;
            }

            netplayManager.SetLocalMode(addr, draw);

            rngService.SetSeed(seed);
            replayService.Initialize();

            StartGame(mode, startLevel, netplayManager);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("replay")]
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

            netplayManager.SetReplayMode();
            replayService.LoadAndStart(replayName);
            TFGamePatch.SetupReplayInputRenderer();
        }

        [Command("online")]
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

            StartGame(mode, 0, netplayManager);

            TFGame.Instance.Commands.Open = false;
        }

        [Command("menu")]
        public static void SwitchToMenu(string[] args)
        {
            var netplayManager = ServiceCollections.ResolveNetplayManager();

            netplayManager.ResetMode();

            TFGame.Instance.Scene = new MainMenu(MainMenu.MenuState.Main);

            TFGame.Instance.Commands.Open = false;
        }

        private static TowerFall.Modes ParseMode(string arg)
        {
            switch (arg)
            {
                case "LMS": return TowerFall.Modes.LastManStanding;
                case "HH": return TowerFall.Modes.HeadHunters;
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

        private static void StartGame(TowerFall.Modes mode, int startLevel, INetplayManager netplayManager)
        {
            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            MatchSettings matchSettings = MatchSettings.GetDefaultVersus();
            matchSettings.Mode = mode;
            (matchSettings.LevelSystem as VersusLevelSystem).StartOnLevel(startLevel);
            var session = new Session(matchSettings);
            netplayManager.Init(session.RoundLogic);
            session.StartGame();
        }
    }
}
